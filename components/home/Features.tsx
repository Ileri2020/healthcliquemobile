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