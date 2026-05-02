import { View, Text } from 'react-native';
import { Link } from 'expo-router';

export default function HelpPage() {
  return (
    <View className="flex-1 items-center justify-center bg-white dark:bg-gray-900">
      <Text className="text-2xl font-bold text-gray-900 dark:text-white">Help Page</Text>
      <Link href="/" className="mt-4 text-blue-500">Go back Home</Link>
    </View>
  );
}