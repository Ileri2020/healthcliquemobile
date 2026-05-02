import { View, Text } from 'react-native';

export default function FeaturedProducts() {
  return (
    <View className="pb-8 bg-white dark:bg-black">
      <Text className="text-xl font-bold text-gray-900 dark:text-white px-4 pt-4">Featured Products</Text>
      <Text className="px-4 text-gray-500 mt-2">See our curated list of top-selling products.</Text>
    </View>
  );
}