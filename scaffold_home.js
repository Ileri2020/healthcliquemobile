const fs = require('fs');
const path = require('path');

const componentsDir = 'c:\\Users\\Ololade\\Desktop\\portfolio\\commerce\\health-clique\\health-clique-mobile\\components\\home';

const components = {
  'Hero': `
import { View, Text, ImageBackground, TouchableOpacity } from 'react-native';
import { Link } from 'expo-router';

export default function Hero() {
  return (
    <ImageBackground 
      source={{ uri: 'https://images.unsplash.com/photo-1576091160550-2173dba999ef?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80' }}
      className="h-[400px] w-full items-start justify-center p-6 bg-gray-900"
      imageStyle={{ opacity: 0.6 }}
    >
      <View className="max-w-[80%]">
        <Text className="text-primary font-bold text-lg mb-2">HealthClique Care</Text>
        <Text className="text-white text-4xl font-extrabold mb-4">Your Health, Delivered Fast & Safely.</Text>
        <Text className="text-gray-200 text-base mb-8">Access essential medications, professional advice, and healthcare support anytime, anywhere.</Text>
        
        <Link href="/store" asChild>
          <TouchableOpacity className="bg-primary px-8 py-3 rounded-full shadow-lg">
            <Text className="text-white font-bold text-lg">Shop Now</Text>
          </TouchableOpacity>
        </Link>
      </View>
    </ImageBackground>
  );
}
`,
  'Features': `
import { View, Text } from 'react-native';

const features = [
  { id: 1, title: 'Trusted Expertise', desc: 'Managed by licensed Pharmacists.', icon: '🛡️' },
  { id: 2, title: 'Fast Delivery', desc: 'Doorstep delivery you can count on.', icon: '🚚' },
  { id: 3, title: 'Genuine Meds', desc: 'Authentic medications directly sourced.', icon: '💊' },
];

export default function Features() {
  return (
    <View className="py-8 px-4 bg-white dark:bg-black">
      <Text className="text-2xl font-bold text-gray-900 dark:text-white mb-6 text-center">Why Choose Us</Text>
      <View className="flex-row flex-wrap justify-between">
        {features.map((f) => (
          <View key={f.id} className="w-[30%] bg-gray-50 dark:bg-gray-900 p-4 rounded-2xl items-center shadow-sm">
            <Text className="text-3xl mb-2">{f.icon}</Text>
            <Text className="font-bold text-center text-sm dark:text-white mb-1">{f.title}</Text>
            <Text className="text-xs text-center text-gray-500 dark:text-gray-400">{f.desc}</Text>
          </View>
        ))}
      </View>
    </View>
  );
}
`,
  'CommonMedications': `
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
`,
  'PartnerBrands': `
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
`,
  'ConcernGrid': `
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
`,
  'FeaturedCategories': `
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
`,
  'HeavilyDiscountedCarousel': `
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
`,
  'FeaturedIngredients': `
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
`,
  'FeaturedProducts': `
import { View, Text } from 'react-native';

export default function FeaturedProducts() {
  return (
    <View className="pb-8 bg-white dark:bg-black">
      <Text className="text-xl font-bold text-gray-900 dark:text-white px-4 pt-4">Featured Products</Text>
      <Text className="px-4 text-gray-500 mt-2">See our curated list of top-selling products.</Text>
    </View>
  );
}
`
};

for (const [name, content] of Object.entries(components)) {
  fs.writeFileSync(path.join(componentsDir, name + '.tsx'), content.trim());
}

console.log("Homepage components created successfully!");
