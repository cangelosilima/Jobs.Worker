import { describe, it, expect, beforeEach } from 'vitest';
import { useNotificationStore } from '../notification.store';

describe('Notification Store', () => {
  beforeEach(() => {
    // Reset store before each test
    useNotificationStore.setState({
      notifications: [],
      unreadCount: 0,
    });
  });

  it('should start with empty notifications', () => {
    const { notifications, unreadCount } = useNotificationStore.getState();

    expect(notifications).toHaveLength(0);
    expect(unreadCount).toBe(0);
  });

  it('should add notification', () => {
    const { addNotification } = useNotificationStore.getState();

    addNotification({
      id: '1',
      timestamp: new Date().toISOString(),
      severity: 'info',
      title: 'Test',
      message: 'Test message',
    });

    const { notifications, unreadCount } = useNotificationStore.getState();

    expect(notifications).toHaveLength(1);
    expect(unreadCount).toBe(1);
    expect(notifications[0].title).toBe('Test');
    expect(notifications[0].read).toBe(false);
  });

  it('should mark notification as read', () => {
    const { addNotification, markAsRead } = useNotificationStore.getState();

    addNotification({
      id: '1',
      timestamp: new Date().toISOString(),
      severity: 'info',
      title: 'Test',
      message: 'Test message',
    });

    markAsRead('1');

    const { notifications, unreadCount } = useNotificationStore.getState();

    expect(notifications[0].read).toBe(true);
    expect(unreadCount).toBe(0);
  });

  it('should mark all notifications as read', () => {
    const { addNotification, markAllAsRead } = useNotificationStore.getState();

    addNotification({
      id: '1',
      timestamp: new Date().toISOString(),
      severity: 'info',
      title: 'Test 1',
      message: 'Test message 1',
    });

    addNotification({
      id: '2',
      timestamp: new Date().toISOString(),
      severity: 'warning',
      title: 'Test 2',
      message: 'Test message 2',
    });

    markAllAsRead();

    const { notifications, unreadCount } = useNotificationStore.getState();

    expect(notifications).toHaveLength(2);
    expect(notifications[0].read).toBe(true);
    expect(notifications[1].read).toBe(true);
    expect(unreadCount).toBe(0);
  });

  it('should remove notification', () => {
    const { addNotification, removeNotification } = useNotificationStore.getState();

    addNotification({
      id: '1',
      timestamp: new Date().toISOString(),
      severity: 'info',
      title: 'Test',
      message: 'Test message',
    });

    removeNotification('1');

    const { notifications } = useNotificationStore.getState();

    expect(notifications).toHaveLength(0);
  });

  it('should clear all notifications', () => {
    const { addNotification, clearAll } = useNotificationStore.getState();

    addNotification({
      id: '1',
      timestamp: new Date().toISOString(),
      severity: 'info',
      title: 'Test 1',
      message: 'Test message 1',
    });

    addNotification({
      id: '2',
      timestamp: new Date().toISOString(),
      severity: 'warning',
      title: 'Test 2',
      message: 'Test message 2',
    });

    clearAll();

    const { notifications, unreadCount } = useNotificationStore.getState();

    expect(notifications).toHaveLength(0);
    expect(unreadCount).toBe(0);
  });

  it('should limit notifications to 100', () => {
    const { addNotification } = useNotificationStore.getState();

    // Add 105 notifications
    for (let i = 0; i < 105; i++) {
      addNotification({
        id: `${i}`,
        timestamp: new Date().toISOString(),
        severity: 'info',
        title: `Test ${i}`,
        message: `Test message ${i}`,
      });
    }

    const { notifications } = useNotificationStore.getState();

    // Should only keep the last 100
    expect(notifications).toHaveLength(100);
  });
});
