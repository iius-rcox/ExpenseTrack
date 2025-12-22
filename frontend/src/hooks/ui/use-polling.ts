import { useEffect, useRef, useCallback, useState } from 'react';

/**
 * Polling Hook for Background Data Refresh
 *
 * Provides 30-second polling for dashboard metrics and activity feeds.
 * Automatically pauses when the tab is not visible (saves resources).
 *
 * NOTE: For TanStack Query, prefer using the built-in refetchInterval option.
 * This hook is for cases where you need more control over polling behavior.
 *
 * @example
 * ```tsx
 * const { isPolling, lastPolledAt, forceRefresh } = usePolling(
 *   fetchDashboardData,
 *   { interval: 30000 }
 * );
 * ```
 */

export interface UsePollingOptions {
  /** Polling interval in milliseconds (default: 30000 = 30 seconds) */
  interval?: number;
  /** Whether polling is enabled (default: true) */
  enabled?: boolean;
  /** Whether to pause polling when tab is not visible (default: true) */
  pauseOnHidden?: boolean;
  /** Whether to poll immediately on mount (default: false) */
  pollOnMount?: boolean;
}

export interface UsePollingReturn {
  /** Whether polling is currently active */
  isPolling: boolean;
  /** When the last successful poll occurred */
  lastPolledAt: Date | null;
  /** When the next poll is scheduled */
  nextPollAt: Date | null;
  /** Force an immediate refresh (resets the interval) */
  forceRefresh: () => void;
  /** Pause polling */
  pause: () => void;
  /** Resume polling */
  resume: () => void;
  /** Any error from the last poll attempt */
  error: Error | null;
}

export function usePolling(
  pollFn: () => void | Promise<void>,
  options: UsePollingOptions = {}
): UsePollingReturn {
  const {
    interval = 30_000, // 30 seconds
    enabled = true,
    pauseOnHidden = true,
    pollOnMount = false,
  } = options;

  const [isPolling, setIsPolling] = useState(enabled);
  const [lastPolledAt, setLastPolledAt] = useState<Date | null>(null);
  const [nextPollAt, setNextPollAt] = useState<Date | null>(null);
  const [error, setError] = useState<Error | null>(null);
  const [isPaused, setIsPaused] = useState(false);

  const pollFnRef = useRef(pollFn);
  pollFnRef.current = pollFn;

  const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const executePoll = useCallback(async () => {
    try {
      setError(null);
      await pollFnRef.current();
      setLastPolledAt(new Date());
      setNextPollAt(new Date(Date.now() + interval));
    } catch (err) {
      setError(err instanceof Error ? err : new Error(String(err)));
    }
  }, [interval]);

  const startPolling = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
    }

    intervalRef.current = setInterval(executePoll, interval);
    setNextPollAt(new Date(Date.now() + interval));
    setIsPolling(true);
  }, [executePoll, interval]);

  const stopPolling = useCallback(() => {
    if (intervalRef.current) {
      clearInterval(intervalRef.current);
      intervalRef.current = null;
    }
    setNextPollAt(null);
    setIsPolling(false);
  }, []);

  const forceRefresh = useCallback(() => {
    executePoll();
    // Reset the interval after a force refresh
    if (enabled && !isPaused) {
      startPolling();
    }
  }, [executePoll, enabled, isPaused, startPolling]);

  const pause = useCallback(() => {
    setIsPaused(true);
    stopPolling();
  }, [stopPolling]);

  const resume = useCallback(() => {
    setIsPaused(false);
    if (enabled) {
      startPolling();
    }
  }, [enabled, startPolling]);

  // Handle enabled state changes
  useEffect(() => {
    if (enabled && !isPaused) {
      if (pollOnMount) {
        executePoll();
      }
      startPolling();
    } else {
      stopPolling();
    }

    return () => {
      stopPolling();
    };
  }, [enabled, isPaused, pollOnMount, executePoll, startPolling, stopPolling]);

  // Handle visibility changes
  useEffect(() => {
    if (!pauseOnHidden) return;

    const handleVisibilityChange = () => {
      if (document.hidden) {
        // Tab is hidden, pause polling
        if (intervalRef.current) {
          clearInterval(intervalRef.current);
          intervalRef.current = null;
          setNextPollAt(null);
        }
      } else {
        // Tab is visible again, resume polling
        if (enabled && !isPaused) {
          // Poll immediately when becoming visible
          executePoll();
          startPolling();
        }
      }
    };

    document.addEventListener('visibilitychange', handleVisibilityChange);
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange);
    };
  }, [pauseOnHidden, enabled, isPaused, executePoll, startPolling]);

  return {
    isPolling,
    lastPolledAt,
    nextPollAt,
    forceRefresh,
    pause,
    resume,
    error,
  };
}

/**
 * Hook to get the time until next poll in a human-readable format
 */
export function useTimeUntilNextPoll(nextPollAt: Date | null): string | null {
  const [timeLeft, setTimeLeft] = useState<string | null>(null);

  useEffect(() => {
    if (!nextPollAt) {
      setTimeLeft(null);
      return;
    }

    const updateTimeLeft = () => {
      const diff = nextPollAt.getTime() - Date.now();
      if (diff <= 0) {
        setTimeLeft('now');
      } else if (diff < 60000) {
        setTimeLeft(`${Math.ceil(diff / 1000)}s`);
      } else {
        setTimeLeft(`${Math.ceil(diff / 60000)}m`);
      }
    };

    updateTimeLeft();
    const interval = setInterval(updateTimeLeft, 1000);
    return () => clearInterval(interval);
  }, [nextPollAt]);

  return timeLeft;
}
