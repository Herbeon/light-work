#include <Arduino.h>
#include <SPI.h>

#define W_REGISTER    0x20
#define R_RX_PAYLOAD  0x61
#define FLUSH_RX      0xE2

#define CONFIG_REG    0x00
#define EN_AA_REG     0x01
#define EN_RXADDR_REG 0x02
#define SETUP_AW_REG  0x03
#define RF_CH_REG     0x05
#define RF_SETUP_REG  0x06
#define STATUS_REG    0x07
#define RX_ADDR_P1    0x0B
#define RX_ADDR_P2    0x0C
#define RX_PW_P1      0x12
#define RX_PW_P2      0x13
#define FIFO_STATUS   0x17

const int SCK_PIN  = 12;
const int MOSI_PIN = 11;
const int MISO_PIN = 10;
const int CE_PIN   = 9;
const int CSN_PIN  = 8;

// ================= TUNING =================
#define CAL_SAMPLES    200      // ~2s of packets, wings must be STILL
#define DEADBAND_DPS   3.0f     // per-axis noise gate after bias removal
#define PEAK_DECAY     0.96f    // ~1s decay at 100 Hz packet rate
#define OBSERVED_MIN   60.0f    // level-scale floor until a real flap lands
#define OBSERVED_DECAY 0.99995f   // half-life ~2.3 min instead of ~14 s
#define LVL_HYST       0.75f    // drop a level only below 75% of its edge
#define STALE_MS       500
// ==========================================

// Must match the transmitter byte for byte
struct __attribute__((packed)) telemetry {
  float telGYx;
  float telGYy;
  float telGYz;
};

struct WingState {
  const char* name;

  bool  calibrated;
  int   calCount;
  float sumX, sumY, sumZ;
  float biasX, biasY, biasZ;

  float x, y, z;            // bias-corrected + deadbanded
  float magnitude;
  float peak;
  float observedMax;        // self-calibrating level scale

  float axisAvgX, axisAvgY, axisAvgZ;   // for auto hinge-axis pick
  int8_t flapDir;
  uint32_t flapCount;

  uint8_t level;            // 0 idle, 1 gentle, 2 moderate, 3 strong
  unsigned long lastSeen;
};

WingState leftwing;
WingState rightwing;
unsigned long printtime = 0;

const char* LEVEL_NAME[] = { "IDLE    ", "GENTLE  ", "MODERATE", "STRONG  " };

// ============================================================
//  nRF24L01 low level
// ============================================================

void writeCommand(uint8_t COM) {
  digitalWrite(CSN_PIN, LOW);
  SPI.transfer(COM);
  digitalWrite(CSN_PIN, HIGH);
}

void writeRegister(uint8_t REG, uint8_t value) {
  digitalWrite(CSN_PIN, LOW);
  SPI.transfer(W_REGISTER | REG);
  SPI.transfer(value);
  digitalWrite(CSN_PIN, HIGH);
}

void writeAddress(uint8_t REG, uint8_t* addr, int size) {
  digitalWrite(CSN_PIN, LOW);
  SPI.transfer(W_REGISTER | REG);
  for (int x = 0; x < size; x++) SPI.transfer(addr[x]);
  digitalWrite(CSN_PIN, HIGH);
}

uint8_t readRegister(uint8_t REG) {
  digitalWrite(CSN_PIN, LOW);
  SPI.transfer(REG);
  uint8_t result = SPI.transfer(0x00);
  digitalWrite(CSN_PIN, HIGH);
  return result;
}

void readPayload(uint8_t* buf, uint8_t len) {
  digitalWrite(CSN_PIN, LOW);
  SPI.transfer(R_RX_PAYLOAD);
  for (uint8_t i = 0; i < len; i++) buf[i] = SPI.transfer(0x00);
  digitalWrite(CSN_PIN, HIGH);
}

// ============================================================
//  Signal processing
// ============================================================

void resetWing(WingState &w, const char* label) {
  w.name = label;
  w.calibrated = false;
  w.calCount = 0;
  w.sumX = w.sumY = w.sumZ = 0;
  w.biasX = w.biasY = w.biasZ = 0;
  w.x = w.y = w.z = 0;
  w.magnitude = w.peak = 0;
  w.observedMax = OBSERVED_MIN;
  w.axisAvgX = w.axisAvgY = w.axisAvgZ = 0;
  w.flapDir = 1;
  w.flapCount = 0;
  w.level = 0;
  w.lastSeen = 0;
}

void processWing(WingState &w, const telemetry &pkt) {
  w.lastSeen = millis();

  // ---- Stage 1: bias calibration ----
  // A stationary MPU does NOT read zero - every chip has a constant
  // manufacturing offset, and it drifts with temperature. Your board
  // sits around 2 dps at rest; the next one will be different. Has to
  // be measured and subtracted or the flap detector sees phantom motion.
  if (!w.calibrated) {
    w.sumX += pkt.telGYx;
    w.sumY += pkt.telGYy;
    w.sumZ += pkt.telGYz;
    w.calCount++;

    if (w.calCount >= CAL_SAMPLES) {
      w.biasX = w.sumX / CAL_SAMPLES;
      w.biasY = w.sumY / CAL_SAMPLES;
      w.biasZ = w.sumZ / CAL_SAMPLES;
      w.calibrated = true;

      Serial.print("[CAL] "); Serial.print(w.name);
      Serial.print(" bias  X: "); Serial.print(w.biasX, 2);
      Serial.print("  Y: ");      Serial.print(w.biasY, 2);
      Serial.print("  Z: ");      Serial.println(w.biasZ, 2);
    }
    return;
  }

  float x = pkt.telGYx - w.biasX;
  float y = pkt.telGYy - w.biasY;
  float z = pkt.telGYz - w.biasZ;

  // ---- Stage 2: deadband ----
  // Hard-zero below the noise floor so a resting wing reads exactly 0.0
  if (fabsf(x) < DEADBAND_DPS) x = 0.0f;
  if (fabsf(y) < DEADBAND_DPS) y = 0.0f;
  if (fabsf(z) < DEADBAND_DPS) z = 0.0f;

  w.x = x; w.y = y; w.z = z;

  // Vector magnitude is orientation-independent, so mounting angle
  // of the MPU on the wing doesn't matter
  w.magnitude = sqrtf(x*x + y*y + z*z);

  // ---- Stage 3: peak hold with decay ----
  w.peak *= PEAK_DECAY;
  if (w.magnitude > w.peak) w.peak = w.magnitude;

  // ---- Stage 4: adaptive level scale ----
  // Track the strongest flap seen and scale thresholds off it, so
  // there's nothing to hand-tune. Slow decay means one accidental
  // slam doesn't permanently raise the bar.
  w.observedMax *= OBSERVED_DECAY;
  if (w.observedMax < OBSERVED_MIN) w.observedMax = OBSERVED_MIN;
  if (w.peak > w.observedMax) w.observedMax = w.peak;

  float gentle   = w.observedMax * 0.15f;
  float moderate = w.observedMax * 0.40f;
  float strong   = w.observedMax * 0.70f;

  uint8_t lvl = 0;
  if      (w.peak >= strong)   lvl = 3;
  else if (w.peak >= moderate) lvl = 2;
  else if (w.peak >= gentle)   lvl = 1;

  // Hysteresis - stops the level flickering at a boundary
  if (lvl < w.level) {
    const float edge[] = { 0, gentle, moderate, strong };
    if (w.peak > edge[w.level] * LVL_HYST) lvl = w.level;
  }
  w.level = lvl;

  // ---- Stage 5: flap counting ----
  // Auto-pick the hinge axis: whichever carries the most motion.
  // Slow average so it can't jump axes mid-flap.
  w.axisAvgX = 0.99f * w.axisAvgX + 0.01f * fabsf(x);
  w.axisAvgY = 0.99f * w.axisAvgY + 0.01f * fabsf(y);
  w.axisAvgZ = 0.99f * w.axisAvgZ + 0.01f * fabsf(z);

  float flapVal = x;
  if (w.axisAvgY > w.axisAvgX && w.axisAvgY >= w.axisAvgZ) flapVal = y;
  else if (w.axisAvgZ > w.axisAvgX && w.axisAvgZ > w.axisAvgY) flapVal = z;

  float flapArm = w.observedMax * 0.25f;
  if (flapArm < 20.0f) flapArm = 20.0f;

  // One flap = a full up-then-down swing. Requiring BOTH directions
  // to break the threshold means a slow lift or a bump can't trigger it.
  if (w.flapDir > 0 && flapVal > flapArm) {
    w.flapDir = -1;
  } else if (w.flapDir < 0 && flapVal < -flapArm) {
    w.flapDir = 1;
    w.flapCount++;
  }
}

void printWing(WingState &w) {
  bool live = (millis() - w.lastSeen) < STALE_MS;

  Serial.print(w.name);
  Serial.print(live ? "  [LINK]  " : "  [STALE] ");

  if (!live) { Serial.println(); return; }

  if (!w.calibrated) {
    Serial.print("calibrating ");
    Serial.print((w.calCount * 100) / CAL_SAMPLES);
    Serial.println("%  - hold still");
    return;
  }

  Serial.print(LEVEL_NAME[w.level]);
  Serial.print(" ");
  for (uint8_t i = 0; i < 3; i++) Serial.print(i < w.level ? '#' : '.');

  Serial.print("  peak ");      Serial.print(w.peak, 1);
  Serial.print("  scale ");     Serial.print(w.observedMax, 0);
  Serial.print("  flaps ");     Serial.print(w.flapCount);
  Serial.print("   X ");  Serial.print(w.x, 1);
  Serial.print("  Y ");   Serial.print(w.y, 1);
  Serial.print("  Z ");   Serial.println(w.z, 1);
}

// ============================================================

void setup() {
  Serial.begin(115200);
  unsigned long start = millis();
  while (!Serial && (millis() - start < 3000)) delay(10);
  delay(2000);

  resetWing(leftwing,  "LEFT ");
  resetWing(rightwing, "RIGHT");

  pinMode(CE_PIN, OUTPUT);
  pinMode(CSN_PIN, OUTPUT);
  digitalWrite(CE_PIN, LOW);
  digitalWrite(CSN_PIN, HIGH);

  SPI.begin(SCK_PIN, MISO_PIN, MOSI_PIN, -1);
  SPI.beginTransaction(SPISettings(4000000, MSBFIRST, SPI_MODE0));
  delay(100);

  uint8_t probe = readRegister(STATUS_REG);
  Serial.print("PRE-INIT STATUS (should be 0x0E): 0x");
  Serial.println(probe, HEX);

  writeRegister(CONFIG_REG, 0x0F);      // PWR_UP + PRIM_RX (bit0 = RX mode)
  delay(5);

  uint8_t readback = readRegister(CONFIG_REG);
  Serial.print("CONFIG READBACK (should be 0x0F): 0x");
  Serial.println(readback, HEX);

  writeRegister(EN_AA_REG, 0x00);       // auto-ack OFF, matches TX
  writeRegister(SETUP_AW_REG, 0x03);    // 5-byte addresses
  writeRegister(RF_CH_REG, 0x4C);       // channel 76, matches TX
  writeRegister(RF_SETUP_REG, 0x06);    // 1 Mbps, 0 dBm

  // Pipe 1 holds the full address. Pipe 2 stores ONE byte and
  // inherits 0xE7E7E7E7 from pipe 1 - hardware constraint.
  uint8_t p1_addr[] = {0x01, 0xE7, 0xE7, 0xE7, 0xE7};
  writeAddress(RX_ADDR_P1, p1_addr, 5);
  writeRegister(RX_ADDR_P2, 0x02);

  writeRegister(RX_PW_P1, sizeof(telemetry));
  writeRegister(RX_PW_P2, sizeof(telemetry));
  writeRegister(EN_RXADDR_REG, 0x06);   // pipes 1+2 on, pipe 0 OFF

  writeCommand(FLUSH_RX);
  writeRegister(STATUS_REG, 0x70);

  // CE is a SWITCH on RX - stays HIGH the entire time we listen
  digitalWrite(CE_PIN, HIGH);
  delayMicroseconds(130);

  Serial.print("PAYLOAD SIZE: ");
  Serial.println(sizeof(telemetry));
  Serial.println("--- Receiver active. Hold wings STILL to calibrate. ---");
  Serial.println("--- Send any character to recalibrate. ---");
}

void loop() {
  // Any serial input triggers a fresh calibration
  if (Serial.available()) {
    while (Serial.available()) Serial.read();
    resetWing(leftwing,  "LEFT ");
    resetWing(rightwing, "RIGHT");
    Serial.println("--- Recalibrating. Hold wings STILL. ---");
  }

  // Drain every packet. RX_EMPTY (bit0) reads 1 when the FIFO is empty.
  while (!(readRegister(FIFO_STATUS) & 0x01)) {
    // Read the pipe BEFORE the payload - RX_P_NO updates when the
    // FIFO head moves, so reading after gives the next packet's pipe
    uint8_t status = readRegister(STATUS_REG);
    uint8_t pipe = (status >> 1) & 0x07;

    telemetry incoming;
    readPayload((uint8_t*)&incoming, sizeof(incoming));
    writeRegister(STATUS_REG, 0x40);    // clear RX_DR (write 1 to clear)

    if (pipe == 1)      processWing(leftwing,  incoming);
    else if (pipe == 2) processWing(rightwing, incoming);
  }

  if (millis() - printtime >= 250) {
    printtime = millis();
    // Serial.println("=== WING TELEMETRY ===");
    printWing(leftwing);
    printWing(rightwing);
    // Serial.println();
  }
}