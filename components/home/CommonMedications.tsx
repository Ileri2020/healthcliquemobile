import { View, Text, ScrollView, Image, TouchableOpacity } from 'react-native';

const meds = [
  { id: 1, name: 'Paracetamol 500mg', price: '₦500', img: 'https://images.unsplash.com/photo-1584308666744-24d5c474f2ae?w=300&q=80' },
  { id: 2, name: 'Vitamin C 1000mg', price: '₦1,200', img: 'https://images.unsplash.com/photo-1620916566398-39f1143ab7be?w=300&q=80' },
  { id: 3, name: 'Ibuprofen 400mg', price: '₦800', img: 'https://images.unsplash.com/photo-1550572017-edb9b4f738a0?w=300&q=80' },
  { id: 4, name: 'Artemether Luma', price: '₦1,500', img: 'https://images.unsplash.com/photo-1471864190281-a93a3070b6de?w=300&q=80' },
];

export default function CommonMedications() {
  return (
    <View className="py-8 bg-gray-50 dark:bg-gray-900">
      <View className="px-4 flex-row justify-between items-center mb-4">
        <Text className="text-xl font-bold text-gray-900 dark:text-white">Common Medications</Text>
        <TouchableOpacity><Text className="text-primary font-semibold">View All</Text></TouchableOpacity>
      </View>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} className="pl-4">
        {meds.map((m) => (
          <View key={m.id} className="w-40 mr-4 bg-white dark:bg-black rounded-2xl overflow-hidden shadow-sm border border-gray-100 dark:border-gray-800">
            <Image source={{ uri: m.img }} className="w-full h-32" />
            <View className="p-3">
              <Text className="font-semibold text-gray-800 dark:text-gray-200" numberOfLines={1}>{m.name}</Text>
              <Text className="text-primary font-bold mt-1">{m.price}</Text>
              <TouchableOpacity className="mt-2 bg-primary/10 py-2 rounded-lg items-center">
                <Text className="text-primary font-semibold text-xs">Add to Cart</Text>
              </TouchableOpacity>
            </View>
          </View>
        ))}
      </ScrollView>
    </View>
  );
}