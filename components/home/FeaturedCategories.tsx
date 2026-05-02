import { View, Text, ScrollView, ImageBackground, TouchableOpacity } from 'react-native';

const cats = [
  { name: 'Supplements', img: 'https://images.unsplash.com/photo-1577401239170-897942555fb3?w=300&q=80' },
  { name: 'Baby Care', img: 'https://images.unsplash.com/photo-1555252333-9f8e92e65df9?w=300&q=80' },
  { name: 'Personal Care', img: 'https://images.unsplash.com/photo-1608248543803-ba4f8c70ae0b?w=300&q=80' },
];

export default function FeaturedCategories() {
  return (
    <View className="py-8 bg-white dark:bg-black">
      <Text className="text-xl font-bold text-gray-900 dark:text-white px-4 mb-4">Featured Categories</Text>
      <ScrollView horizontal showsHorizontalScrollIndicator={false} className="pl-4">
        {cats.map((c, i) => (
          <TouchableOpacity key={i} className="mr-4 w-48 h-32 rounded-2xl overflow-hidden">
            <ImageBackground source={{ uri: c.img }} className="w-full h-full justify-end p-4 bg-black/40" imageStyle={{ opacity: 0.7 }}>
              <Text className="text-white font-bold text-lg">{c.name}</Text>
            </ImageBackground>
          </TouchableOpacity>
        ))}
      </ScrollView>
    </View>
  );
}