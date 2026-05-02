import { View, Text, TouchableOpacity } from 'react-native';

export default function HeavilyDiscountedCarousel() {
  return (
    <View className="my-6 mx-4 bg-primary/10 rounded-2xl p-6 border border-primary/20 items-center">
      <Text className="text-primary font-black text-2xl mb-2">🔥 Flash Sale!</Text>
      <Text className="text-gray-700 dark:text-gray-300 text-center mb-4">Get up to 50% off on selected health essentials today.</Text>
      <TouchableOpacity className="bg-primary px-6 py-2 rounded-full">
        <Text className="text-white font-bold">View Offers</Text>
      </TouchableOpacity>
    </View>
  );
}