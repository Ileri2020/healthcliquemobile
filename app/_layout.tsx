import "../global.css";
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

export default function RootLayout() {
  return (
    <>
      <Stack>
        <Stack.Screen name="index" options={{ headerShown: false }} />
        <Stack.Screen name="about" options={{ title: 'About' }} />
        <Stack.Screen name="account" options={{ title: 'Account' }} />
        <Stack.Screen name="admin" options={{ title: 'Admin' }} />
        <Stack.Screen name="blog" options={{ title: 'Blog' }} />
        <Stack.Screen name="cart" options={{ title: 'Cart' }} />
        <Stack.Screen name="contact" options={{ title: 'Contact' }} />
        <Stack.Screen name="help" options={{ title: 'Help' }} />
        <Stack.Screen name="login" options={{ title: 'Login' }} />
        <Stack.Screen name="lunch" options={{ title: 'Lunch' }} />
        <Stack.Screen name="privacy" options={{ title: 'Privacy' }} />
        <Stack.Screen name="products" options={{ title: 'Products' }} />
        <Stack.Screen name="signup" options={{ title: 'Signup' }} />
        <Stack.Screen name="store" options={{ title: 'Store' }} />
        <Stack.Screen name="terms" options={{ title: 'Terms' }} />
      </Stack>
      <StatusBar style="auto" />
    </>
  );
}