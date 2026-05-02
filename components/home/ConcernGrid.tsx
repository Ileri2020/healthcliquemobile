import { View, Text, TouchableOpacity } from 'react-native';

const concerns = ['Pain Relief', 'Cold & Cough', 'Digestive Health', 'Vitamins', 'Skin Care', 'First Aid'];

export default function ConcernGrid() {
  return (
    <View className="py-8 px-4 bg-gray-50 dark:bg-gray-900">
      <Text className="text-xl font-bold text-gray-900 dark:text-white mb-4">Shop By Concern</Text>
      <View className="flex-row flex-wrap justify-between">
        {concerns.map((c, i) => (
          <TouchableOpacity key={i} className="w-[48%] bg-white dark:bg-black p-4 rounded-xl mb-3 shadow-sm border border-gray-100 dark:border-gray-800">
            <Text className="font-semibold text-center dark:text-white">{c}</Text>
          </TouchableOpacity>
        ))}
      </View>
    </View>
  );
}