const fs = require('fs');
const path = require('path');

const projectDir = 'c:\\Users\\Ololade\\Desktop\\portfolio\\commerce\\health-clique\\health-clique-mobile';
const oguduDir = 'c:\\Users\\Ololade\\Desktop\\portfolio\\ogudu\\reactNativeTemp';

// 1. Copy config files
const configFiles = ['tailwind.config.js', 'babel.config.js', 'metro.config.js', 'global.css'];
for (const file of configFiles) {
    const src = path.join(oguduDir, file);
    const dest = path.join(projectDir, file);
    if (fs.existsSync(src)) {
        fs.copyFileSync(src, dest);
    }
}

// 2. Create app directory
const appDir = path.join(projectDir, 'app');
if (!fs.existsSync(appDir)) {
    fs.mkdirSync(appDir, { recursive: true });
}

// 3. Create placeholder pages
const pages = [
    'about', 'account', 'admin', 'blog', 'cart', 'contact', 'help', 'login', 'lunch', 'privacy', 'products', 'signup', 'store', 'terms'
];

pages.forEach(page => {
    const content = `
import { View, Text } from 'react-native';
import { Link } from 'expo-router';

export default function ${page.charAt(0).toUpperCase() + page.slice(1)}Page() {
  return (
    <View className="flex-1 items-center justify-center bg-white dark:bg-gray-900">
      <Text className="text-2xl font-bold text-gray-900 dark:text-white">${page.charAt(0).toUpperCase() + page.slice(1)} Page</Text>
      <Link href="/" className="mt-4 text-blue-500">Go back Home</Link>
    </View>
  );
}
`;
    fs.writeFileSync(path.join(appDir, `${page}.tsx`), content.trim());
});

// 4. Create _layout.tsx
const layoutContent = `
import "../global.css";
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

export default function RootLayout() {
  return (
    <>
      <Stack>
        <Stack.Screen name="index" options={{ headerShown: false }} />
        ${pages.map(page => `<Stack.Screen name="${page}" options={{ title: '${page.charAt(0).toUpperCase() + page.slice(1)}' }} />`).join('\n        ')}
      </Stack>
      <StatusBar style="auto" />
    </>
  );
}
`;
fs.writeFileSync(path.join(appDir, '_layout.tsx'), layoutContent.trim());

// 5. Create Components Directory
const componentsDir = path.join(projectDir, 'components', 'home');
if (!fs.existsSync(componentsDir)) {
    fs.mkdirSync(componentsDir, { recursive: true });
}

// Write some dummy components for the home page
const homeComponents = [
    'Hero', 'CommonMedications', 'HeavilyDiscountedCarousel', 'PartnerBrands', 
    'ConcernGrid', 'FeaturedCategories', 'FeaturedIngredients', 'FeaturedProducts', 'Features'
];

homeComponents.forEach(comp => {
    const compContent = `
import { View, Text } from 'react-native';

export default function ${comp}() {
  return (
    <View className="py-8 px-4 border-b border-gray-200 dark:border-gray-800">
      <Text className="text-xl font-semibold text-center text-gray-800 dark:text-gray-200">${comp} Component</Text>
    </View>
  );
}
`;
    fs.writeFileSync(path.join(componentsDir, `${comp}.tsx`), compContent.trim());
});

// 6. Create Home/Index Page
const indexContent = `
import { ScrollView, View, Text, TouchableOpacity } from 'react-native';
import { Link } from 'expo-router';
import Hero from '../components/home/Hero';
import CommonMedications from '../components/home/CommonMedications';
import HeavilyDiscountedCarousel from '../components/home/HeavilyDiscountedCarousel';
import PartnerBrands from '../components/home/PartnerBrands';
import ConcernGrid from '../components/home/ConcernGrid';
import FeaturedCategories from '../components/home/FeaturedCategories';
import FeaturedIngredients from '../components/home/FeaturedIngredients';
import FeaturedProducts from '../components/home/FeaturedProducts';
import Features from '../components/home/Features';

export default function Home() {
  return (
    <View className="flex-1 bg-white dark:bg-black">
      <ScrollView className="flex-1 w-full" contentContainerStyle={{ paddingBottom: 100 }}>
        <Hero />
        
        {/* Page Links Section */}
        <View className="p-4 bg-gray-50 dark:bg-gray-900 my-4 rounded-xl mx-4">
          <Text className="text-lg font-bold mb-4 text-gray-900 dark:text-white">Navigation Links</Text>
          <View className="flex-row flex-wrap gap-2">
            ${pages.map(page => `<Link href="/${page}" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">${page}</Text></TouchableOpacity></Link>`).join('\n            ')}
          </View>
        </View>

        <CommonMedications />
        <HeavilyDiscountedCarousel />
        <PartnerBrands />
        <ConcernGrid />
        <FeaturedCategories />
        <FeaturedIngredients />
        <FeaturedProducts />
        <Features />
      </ScrollView>

      {/* Floating Contact Button */}
      <Link href="/contact" asChild>
        <TouchableOpacity className="absolute bottom-6 left-6 w-14 h-14 bg-primary rounded-full items-center justify-center shadow-lg border-2 border-white dark:border-gray-800">
          <Text className="text-white text-2xl">💬</Text>
        </TouchableOpacity>
      </Link>
    </View>
  );
}
`;
fs.writeFileSync(path.join(appDir, 'index.tsx'), indexContent.trim());

console.log("All pages and configs successfully generated!");
