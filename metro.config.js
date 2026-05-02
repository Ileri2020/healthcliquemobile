const { getDefaultConfig } = require("expo/metro-config");

const config = getDefaultConfig(__dirname);

try {
  const { withNativeWind } = require("nativewind/metro");
  module.exports = withNativeWind(config, { input: "./global.css" });
} catch (e) {
  console.warn("NativeWind could not be loaded into Metro config. Falling back to default config.");
  module.exports = config;
}