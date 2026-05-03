import React, { useState } from 'react';
import { View, Text, TouchableOpacity, Modal, StyleSheet, Pressable, Image, Appearance, useColorScheme } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';
import { MaterialCommunityIcons } from '@expo/vector-icons';
import { useRouter } from 'expo-router';

export default function Header({ title }: { title?: string }) {
  const router = useRouter();
  const colorScheme = useColorScheme() ?? 'light';
  const [menuVisible, setMenuVisible] = useState(false);

  const toggleTheme = () => {
    const newTheme = colorScheme === 'light' ? 'dark' : 'light';
    Appearance.setColorScheme(newTheme);
    setMenuVisible(false);
  };

  const navigateTo = (path: string) => {
    router.push(path as any);
    setMenuVisible(false);
  };

  const isDark = colorScheme === 'dark';
  const textColor = isDark ? '#FFFFFF' : '#000000';
  const bgColor = isDark ? '#1A1A1A' : '#FFFFFF';
  const borderColor = isDark ? '#333333' : '#E5E5E5';

  return (
    <SafeAreaView edges={['top']} style={[styles.safeArea, { backgroundColor: bgColor }]}>
      <View style={[styles.container, { borderBottomColor: borderColor }]}>
        <TouchableOpacity onPress={() => router.replace('/')} style={styles.logoContainer}>
          <Image 
            source={require('../assets/images/icon.png')} 
            style={styles.logoImage}
            resizeMode="contain"
          />
          <Text style={[styles.logoText, { color: textColor }]}>HealthClique</Text>
        </TouchableOpacity>

        <TouchableOpacity onPress={() => setMenuVisible(true)} style={styles.menuButton}>
          <MaterialCommunityIcons name="dots-vertical" size={24} color={textColor} />
        </TouchableOpacity>
      </View>

      <Modal
        visible={menuVisible}
        transparent={true}
        animationType="fade"
        onRequestClose={() => setMenuVisible(false)}
      >
        <Pressable style={styles.modalOverlay} onPress={() => setMenuVisible(false)}>
          <View style={[styles.menuDropdown, { backgroundColor: bgColor, borderBottomColor: borderColor }]}>
            <TouchableOpacity style={styles.menuItem} onPress={() => navigateTo('/cart')}>
              <MaterialCommunityIcons name="cart" size={20} color={textColor} style={styles.menuIcon} />
              <Text style={[styles.menuText, { color: textColor }]}>My Cart</Text>
            </TouchableOpacity>

            <TouchableOpacity style={styles.menuItem} onPress={() => navigateTo('/store')}>
              <MaterialCommunityIcons name="storefront" size={20} color={textColor} style={styles.menuIcon} />
              <Text style={[styles.menuText, { color: textColor }]}>Store</Text>
            </TouchableOpacity>
            
            <TouchableOpacity style={styles.menuItem} onPress={() => navigateTo('/login')}>
              <MaterialCommunityIcons name="login" size={20} color={textColor} style={styles.menuIcon} />
              <Text style={[styles.menuText, { color: textColor }]}>Login</Text>
            </TouchableOpacity>

            <View style={[styles.separator, { backgroundColor: borderColor }]} />

            <TouchableOpacity style={styles.menuItem} onPress={() => navigateTo('/terms')}>
              <Text style={[styles.menuText, { color: textColor, paddingLeft: 32 }]}>Terms of Service</Text>
            </TouchableOpacity>

            <TouchableOpacity style={styles.menuItem} onPress={toggleTheme}>
              <MaterialCommunityIcons 
                name={isDark ? "white-balance-sunny" : "moon-waning-crescent"} 
                size={20} 
                color={textColor} 
                style={styles.menuIcon} 
              />
              <Text style={[styles.menuText, { color: textColor }]}>
                {isDark ? 'Light Mode' : 'Dark Mode'}
              </Text>
            </TouchableOpacity>
          </View>
        </Pressable>
      </Modal>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safeArea: {
    zIndex: 100,
  },
  container: {
    height: 60,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    borderBottomWidth: 1,
  },
  logoContainer: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  logoImage: {
    width: 32,
    height: 32,
    marginRight: 8,
    borderRadius: 8,
  },
  logoText: {
    fontSize: 20,
    fontWeight: '800',
    letterSpacing: -0.5,
  },
  menuButton: {
    padding: 8,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.1)',
  },
  menuDropdown: {
    position: 'absolute',
    top: 60,
    right: 16,
    width: 220,
    borderRadius: 12,
    paddingVertical: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 12,
    elevation: 5,
    borderWidth: 1,
    borderColor: 'rgba(0,0,0,0.05)',
  },
  menuItem: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingVertical: 12,
    paddingHorizontal: 16,
  },
  menuIcon: {
    marginRight: 12,
  },
  menuText: {
    fontSize: 16,
    fontWeight: '500',
  },
  separator: {
    height: 1,
    marginVertical: 4,
  },
});
