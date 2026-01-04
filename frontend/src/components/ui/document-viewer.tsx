'use client';

/**
 * DocumentViewer Component
 *
 * A universal document viewer that handles both images and PDFs.
 * Uses native browser capabilities for PDF rendering via iframe.
 *
 * Features:
 * - Automatic format detection via contentType or file extension
 * - Zoom and rotation controls for images
 * - Native PDF viewing with browser's built-in viewer
 * - Fallback state for unsupported formats
 */

import { useState, useMemo, useCallback } from 'react';
import { cn } from '@/lib/utils';
import {
  ZoomIn,
  ZoomOut,
  RotateCw,
  Maximize2,
  FileText,
  AlertTriangle,
  ExternalLink,
  RefreshCw,
} from 'lucide-react';
import { Button } from './button';
import { Separator } from './separator';

interface DocumentViewerProps {
  /** URL to the document */
  src: string | null | undefined;
  /** MIME type of the document (e.g., 'application/pdf', 'image/jpeg') */
  contentType?: string | null;
  /** Original filename (used for format detection fallback) */
  filename?: string | null;
  /** Alt text for images */
  alt?: string;
  /** Whether to show zoom/rotation controls (images only) */
  showControls?: boolean;
  /** Custom class name for the container */
  className?: string;
  /** Custom class name for the document element */
  documentClassName?: string;
  /** Callback when external link is clicked */
  onOpenExternal?: () => void;
}

type DocumentType = 'image' | 'pdf' | 'unknown';

/**
 * Determines the document type from contentType or filename.
 */
function getDocumentType(contentType?: string | null, filename?: string | null): DocumentType {
  // Check contentType first
  if (contentType) {
    if (contentType.startsWith('image/')) return 'image';
    if (contentType === 'application/pdf') return 'pdf';
  }

  // Fallback to filename extension
  if (filename) {
    const ext = filename.toLowerCase().split('.').pop();
    if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'heic', 'heif', 'bmp'].includes(ext || '')) {
      return 'image';
    }
    if (ext === 'pdf') return 'pdf';
  }

  return 'unknown';
}

export function DocumentViewer({
  src,
  contentType,
  filename,
  alt = 'Document',
  showControls = true,
  className,
  documentClassName,
  onOpenExternal,
}: DocumentViewerProps) {
  // Image viewer state
  const [zoom, setZoom] = useState(1);
  const [rotation, setRotation] = useState(0);

  // Error state with retry key
  const [loadError, setLoadError] = useState(false);
  const [retryKey, setRetryKey] = useState(0);

  const documentType = useMemo(
    () => getDocumentType(contentType, filename),
    [contentType, filename]
  );

  // Image controls
  const handleZoomIn = () => setZoom((z) => Math.min(z + 0.25, 3));
  const handleZoomOut = () => setZoom((z) => Math.max(z - 0.25, 0.5));
  const handleRotate = () => setRotation((r) => (r + 90) % 360);
  const handleReset = () => {
    setZoom(1);
    setRotation(0);
  };

  // Error handling with retry
  const handleLoadError = useCallback(() => {
    setLoadError(true);
  }, []);

  const handleRetry = useCallback(() => {
    setLoadError(false);
    setRetryKey((k) => k + 1);
  }, []);

  // No source provided
  if (!src) {
    return (
      <div className={cn('flex flex-col items-center justify-center h-full text-muted-foreground', className)}>
        <FileText className="h-16 w-16 mb-2 opacity-50" />
        <span className="text-sm">No document available</span>
      </div>
    );
  }

  // PDF rendering
  if (documentType === 'pdf') {
    return (
      <div className={cn('flex flex-col h-full', className)}>
        {showControls && (
          <div className="flex items-center justify-end gap-1 p-2 border-b bg-muted/30">
            <Button
              variant="ghost"
              size="sm"
              className="h-8"
              onClick={() => {
                window.open(src, '_blank', 'noopener,noreferrer');
                onOpenExternal?.();
              }}
            >
              <ExternalLink className="h-4 w-4 mr-1" />
              Open in New Tab
            </Button>
          </div>
        )}
        <div className="flex-1 min-h-0">
          <iframe
            src={src}
            title={alt}
            className={cn('w-full h-full border-0', documentClassName)}
            style={{ minHeight: '500px' }}
          />
        </div>
      </div>
    );
  }

  // Image rendering
  if (documentType === 'image') {
    // Show error state with retry option
    if (loadError) {
      return (
        <div className={cn('flex flex-col items-center justify-center h-full text-muted-foreground', className)}>
          <AlertTriangle className="h-12 w-12 mb-3 text-amber-500" />
          <span className="text-sm font-medium mb-1">Failed to load document</span>
          <span className="text-xs text-muted-foreground mb-4">
            The image could not be loaded. Please check your connection and try again.
          </span>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={handleRetry}>
              <RefreshCw className="h-4 w-4 mr-1" />
              Retry
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => {
                window.open(src, '_blank', 'noopener,noreferrer');
                onOpenExternal?.();
              }}
            >
              <ExternalLink className="h-4 w-4 mr-1" />
              Open in Browser
            </Button>
          </div>
        </div>
      );
    }

    return (
      <div className={cn('flex flex-col h-full', className)}>
        {showControls && (
          <div className="flex items-center justify-end gap-1 p-2 border-b bg-muted/30">
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={handleZoomOut}
              disabled={zoom <= 0.5}
            >
              <ZoomOut className="h-4 w-4" />
            </Button>
            <span className="text-xs text-muted-foreground w-12 text-center">
              {Math.round(zoom * 100)}%
            </span>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={handleZoomIn}
              disabled={zoom >= 3}
            >
              <ZoomIn className="h-4 w-4" />
            </Button>
            <Separator orientation="vertical" className="h-4 mx-1" />
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={handleRotate}
            >
              <RotateCw className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8"
              onClick={handleReset}
            >
              <Maximize2 className="h-4 w-4" />
            </Button>
          </div>
        )}
        <div className="flex-1 overflow-auto min-h-0">
          <div
            className="min-h-full min-w-full flex items-center justify-center p-4"
            style={{
              transform: `scale(${zoom}) rotate(${rotation}deg)`,
              transformOrigin: 'center center',
              transition: 'transform 0.2s ease',
            }}
          >
            <img
              key={retryKey}
              src={src}
              alt={alt}
              className={cn('max-w-full max-h-full object-contain shadow-lg rounded', documentClassName)}
              draggable={false}
              onError={handleLoadError}
            />
          </div>
        </div>
      </div>
    );
  }

  // Unknown format fallback
  return (
    <div className={cn('flex flex-col items-center justify-center h-full text-muted-foreground', className)}>
      <AlertTriangle className="h-12 w-12 mb-2 opacity-50" />
      <span className="text-sm">Unsupported document format</span>
      {src && (
        <Button
          variant="outline"
          size="sm"
          className="mt-4"
          onClick={() => {
            window.open(src, '_blank', 'noopener,noreferrer');
            onOpenExternal?.();
          }}
        >
          <ExternalLink className="h-4 w-4 mr-1" />
          Open in Browser
        </Button>
      )}
    </div>
  );
}

/**
 * Skeleton loader for DocumentViewer
 */
export function DocumentViewerSkeleton({ className }: { className?: string }) {
  return (
    <div className={cn('flex flex-col h-full', className)}>
      <div className="flex items-center justify-end gap-1 p-2 border-b bg-muted/30">
        <div className="h-8 w-8 rounded bg-muted animate-pulse" />
        <div className="h-8 w-8 rounded bg-muted animate-pulse" />
        <div className="h-8 w-8 rounded bg-muted animate-pulse" />
      </div>
      <div className="flex-1 flex items-center justify-center p-4">
        <div className="w-full h-full rounded-lg bg-muted animate-pulse" />
      </div>
    </div>
  );
}
