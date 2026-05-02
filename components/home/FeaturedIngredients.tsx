import { View, Text, ScrollView } from 'react-native';

const ingredients = ['Biotin', 'Collagen', 'Zinc', 'Magnesium', 'Omega-3', 'Retinol'];

export default function FeaturedIngredients() {
  return (
    <View className="py-6 px-4 bg-white dark:bg-black">
      <Text className="text-lg font-bold text-gray-900 dark:text-white mb-3">Trending Ingredients</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} className="flex-row">
        {ingredients.map((ing, i) => (
          <View key={i} className="bg-gray-100 dark:bg-gray-800 px-5 py-2 rounded-full mr-3">
            <Text className="font-medium text-gray-800 dark:text-gray-200">{ing}</Text>
          </View>
        ))}
      </ScrollView>
    </View>
  );
}