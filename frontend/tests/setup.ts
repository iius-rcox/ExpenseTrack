import '@testing-library/jest-dom';
import { vi } from 'vitest';
import React from 'react';

// Global test setup for the ExpenseFlow frontend redesign
// This file runs before each test file and sets up the testing environment

// Extend Vitest's expect with jest-dom matchers for better DOM assertions
// This enables assertions like: expect(element).toBeInTheDocument()

// Mock TanStack Router globally for all tests
// This is needed because many components use Link which requires router context
vi.mock('@tanstack/react-router', () => {
  // Create a simple functional component for Link
  const MockLink = ({ children, to, className, ...props }: {
    children?: React.ReactNode;
    to?: string;
    className?: string;
    [key: string]: unknown;
  }) => {
    return React.createElement('a', { ...props, href: to, className }, children);
  };

  return {
    Link: MockLink,
    useNavigate: () => vi.fn(),
    useLocation: () => ({ pathname: '/' }),
    useRouter: () => ({ state: { location: { pathname: '/' } } }),
    useParams: () => ({}),
    useSearch: () => ({}),
    createFileRoute: () => () => ({}),
  };
})

// Mock framer-motion globally for all tests
// This prevents animation-related issues in tests
vi.mock('framer-motion', () => {
  // Helper to create motion element factories that strip animation props
  const createMotionComponent = (type: string) => {
    return function MockMotionComponent({
      children,
      initial,
      animate,
      exit,
      variants,
      whileHover,
      whileTap,
      whileFocus,
      whileInView,
      transition,
      layout,
      layoutId,
      ...props
    }: React.HTMLAttributes<HTMLElement> & Record<string, unknown>) {
      return React.createElement(type, props, children);
    };
  };

  return {
    motion: {
      div: createMotionComponent('div'),
      span: createMotionComponent('span'),
      button: createMotionComponent('button'),
      ul: createMotionComponent('ul'),
      li: createMotionComponent('li'),
      a: createMotionComponent('a'),
      p: createMotionComponent('p'),
      h1: createMotionComponent('h1'),
      h2: createMotionComponent('h2'),
      h3: createMotionComponent('h3'),
    },
    AnimatePresence: ({ children }: { children: React.ReactNode }) => children,
    useAnimation: () => ({ start: vi.fn() }),
    useMotionValue: (val: number) => ({ get: () => val, set: vi.fn() }),
    useTransform: (val: unknown, fn: (v: unknown) => unknown) => ({ get: () => fn(val) }),
  };
})

// Mock window.matchMedia for components that use media queries
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});

// Mock ResizeObserver for components that observe element size
global.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
};
