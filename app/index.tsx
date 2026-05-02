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
            <Link href="/about" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">about</Text></TouchableOpacity></Link>
            <Link href="/account" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">account</Text></TouchableOpacity></Link>
            <Link href="/admin" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">admin</Text></TouchableOpacity></Link>
            <Link href="/blog" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">blog</Text></TouchableOpacity></Link>
            <Link href="/cart" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">cart</Text></TouchableOpacity></Link>
            <Link href="/contact" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">contact</Text></TouchableOpacity></Link>
            <Link href="/help" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">help</Text></TouchableOpacity></Link>
            <Link href="/login" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">login</Text></TouchableOpacity></Link>
            <Link href="/lunch" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">lunch</Text></TouchableOpacity></Link>
            <Link href="/privacy" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">privacy</Text></TouchableOpacity></Link>
            <Link href="/products" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">products</Text></TouchableOpacity></Link>
            <Link href="/signup" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">signup</Text></TouchableOpacity></Link>
            <Link href="/store" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">store</Text></TouchableOpacity></Link>
            <Link href="/terms" asChild><TouchableOpacity className="bg-primary/10 px-4 py-2 rounded-full mb-2 mr-2"><Text className="text-primary font-medium">terms</Text></TouchableOpacity></Link>
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