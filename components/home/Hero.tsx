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