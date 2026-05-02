import { View, Text, ScrollView, Image } from 'react-native';

const brands = [
  'https://images.unsplash.com/photo-1563203369-26f2e4a5ccf7?w=100&q=80',
  'https://images.unsplash.com/photo-1611078489935-0cb964de46d6?w=100&q=80',
  'https://images.unsplash.com/photo-1505751172876-fa1923c5c528?w=100&q=80',
];

export default function PartnerBrands() {
  return (
    <View className="py-8 bg-white dark:bg-black px-4">
      <Text className="text-center font-semibold text-gray-500 mb-4 uppercase tracking-widest text-xs">Trusted By Top Brands</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} className="flex-row">
        {brands.map((b, i) => (
          <Image key={i} source={{ uri: b }} className="w-24 h-12 mr-6 rounded bg-gray-100 opacity-60" resizeMode="contain" />
        ))}
      </ScrollView>
    </View>
  );
}