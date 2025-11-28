import { describe, it, expect, beforeEach } from 'vitest';
import { useAuthStore } from '../auth.store';
import { UserRole } from '@/api/types';

describe('Auth Store', () => {
  beforeEach(() => {
    // Reset store before each test
    useAuthStore.setState({
      user: {
        id: 'default-admin',
        name: 'Admin User',
        email: 'admin@jobscheduler.local',
        roles: [UserRole.Admin, UserRole.Operator, UserRole.JobOwner, UserRole.Viewer],
      },
      isAuthenticated: true,
    });
  });

  it('should have default user logged in', () => {
    const { user, isAuthenticated } = useAuthStore.getState();

    expect(isAuthenticated).toBe(true);
    expect(user).toBeDefined();
    expect(user?.name).toBe('Admin User');
  });

  it('should have all roles assigned to default user', () => {
    const { user } = useAuthStore.getState();

    expect(user?.roles).toContain(UserRole.Admin);
    expect(user?.roles).toContain(UserRole.Operator);
    expect(user?.roles).toContain(UserRole.JobOwner);
    expect(user?.roles).toContain(UserRole.Viewer);
  });

  it('should allow editing (canEdit returns true)', () => {
    const { canEdit } = useAuthStore.getState();

    expect(canEdit()).toBe(true);
  });

  it('should allow deleting (canDelete returns true)', () => {
    const { canDelete } = useAuthStore.getState();

    expect(canDelete()).toBe(true);
  });

  it('should allow triggering (canTrigger returns true)', () => {
    const { canTrigger } = useAuthStore.getState();

    expect(canTrigger()).toBe(true);
  });

  it('should check if user has specific role', () => {
    const { hasRole } = useAuthStore.getState();

    expect(hasRole(UserRole.Admin)).toBe(true);
    expect(hasRole(UserRole.Operator)).toBe(true);
  });

  it('should check if user has any of the specified roles', () => {
    const { hasAnyRole } = useAuthStore.getState();

    expect(hasAnyRole([UserRole.Admin])).toBe(true);
    expect(hasAnyRole([UserRole.Operator, UserRole.Viewer])).toBe(true);
  });

  it('should update user on login', () => {
    const { login } = useAuthStore.getState();

    const newUser = {
      id: 'test-user',
      name: 'Test User',
      email: 'test@example.com',
      roles: [UserRole.Viewer],
    };

    login(newUser);

    const { user } = useAuthStore.getState();
    expect(user?.name).toBe('Test User');
    expect(user?.roles).toContain(UserRole.Viewer);
  });

  it('should reset to default user on logout', () => {
    const { logout } = useAuthStore.getState();

    logout();

    const { user, isAuthenticated } = useAuthStore.getState();
    expect(isAuthenticated).toBe(true);
    expect(user?.name).toBe('Admin User');
  });
});
