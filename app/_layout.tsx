import "../global.css";
import { Stack } from 'expo-router';
import { StatusBar } from 'expo-status-bar';

import Header from '../components/Header';

export default function RootLayout() {
  return (
    <>
      <Stack
        screenOptions={{
          header: () => <Header />,
          headerShown: true,
        }}
      >
        <Stack.Screen name="index" />
        <Stack.Screen name="about" />
        <Stack.Screen name="account" />
        <Stack.Screen name="admin" />
        <Stack.Screen name="blog" />
        <Stack.Screen name="cart" />
        <Stack.Screen name="contact" />
        <Stack.Screen name="help" />
        <Stack.Screen name="login" />
        <Stack.Screen name="lunch" />
        <Stack.Screen name="privacy" />
        <Stack.Screen name="products" />
        <Stack.Screen name="signup" />
        <Stack.Screen name="store" />
        <Stack.Screen name="terms" />
      </Stack>
      <StatusBar style="auto" />
    </>
  );
}