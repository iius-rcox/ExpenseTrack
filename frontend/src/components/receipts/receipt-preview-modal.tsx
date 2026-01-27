'use client';

import { useCallback, useEffect, useRef } from 'react';
import { TransformWrapper, TransformComponent, useControls } from 'react-zoom-pan-pinch';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { ZoomIn, ZoomOut, RotateCcw, Download, Maximize2 } from 'lucide-react';
import { cn } from '@/lib/utils';

interface ReceiptPreviewModalProps {
  /** Whether the modal is open */
  open: boolean;
  /** Callback when modal should close */
  onOpenChange: (open: boolean) => void;
  /** URL of the receipt image to display */
  imageUrl: string;
  /** Alt text for the image */
  alt?: string;
  /** Original filename for download */
  filename?: string;
  /** URL to original full-size image (for download) */
  fullSizeUrl?: string;
}

/**
 * Zoom controls component rendered inside TransformWrapper context.
 */
function ZoomControls() {
  const { zoomIn, zoomOut, resetTransform } = useControls();

  return (
    <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex items-center gap-1 bg-background/90 backdrop-blur-sm rounded-lg border p-1 shadow-lg">
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8"
        onClick={() => zoomOut()}
        title="Zoom out (or press -)"
      >
        <ZoomOut className="h-4 w-4" />
      </Button>
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8"
        onClick={() => resetTransform()}
        title="Reset zoom (or press 0)"
      >
        <RotateCcw className="h-4 w-4" />
      </Button>
      <Button
        variant="ghost"
        size="icon"
        className="h-8 w-8"
        onClick={() => zoomIn()}
        title="Zoom in (or press +)"
      >
        <ZoomIn className="h-4 w-4" />
      </Button>
    </div>
  );
}

/**
 * Receipt preview modal with zoom and pan functionality.
 * Displays receipt images with interactive zoom controls and keyboard navigation.
 */
export function ReceiptPreviewModal({
  open,
  onOpenChange,
  imageUrl,
  alt = 'Receipt preview',
  filename,
  fullSizeUrl,
}: ReceiptPreviewModalProps) {
  const transformRef = useRef<{ zoomIn: () => void; zoomOut: () => void; resetTransform: () => void } | null>(null);

  // Keyboard navigation
  const handleKeyDown = useCallback(
    (e: KeyboardEvent) => {
      if (!open) return;

      switch (e.key) {
        case 'Escape':
          onOpenChange(false);
          break;
        case '+':
        case '=':
          e.preventDefault();
          transformRef.current?.zoomIn();
          break;
        case '-':
        case '_':
          e.preventDefault();
          transformRef.current?.zoomOut();
          break;
        case '0':
          e.preventDefault();
          transformRef.current?.resetTransform();
          break;
      }
    },
    [open, onOpenChange]
  );

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [handleKeyDown]);

  const handleDownload = () => {
    const url = fullSizeUrl || imageUrl;
    const link = document.createElement('a');
    link.href = url;
    link.download = filename || 'receipt';
    link.target = '_blank';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleOpenFullSize = () => {
    const url = fullSizeUrl || imageUrl;
    window.open(url, '_blank');
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl w-[95vw] h-[90vh] p-0 flex flex-col">
        <DialogHeader className="p-4 pb-2 border-b shrink-0">
          <div className="flex items-center justify-between pr-8">
            <div>
              <DialogTitle className="text-base">Receipt Preview</DialogTitle>
              {filename && (
                <DialogDescription className="text-xs truncate max-w-md">
                  {filename}
                </DialogDescription>
              )}
            </div>
            <div className="flex items-center gap-1">
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                onClick={handleOpenFullSize}
                title="Open full size in new tab"
              >
                <Maximize2 className="h-4 w-4" />
              </Button>
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                onClick={handleDownload}
                title="Download original"
              >
                <Download className="h-4 w-4" />
              </Button>
            </div>
          </div>
        </DialogHeader>

        <div className="flex-1 relative overflow-hidden bg-muted/30">
          <TransformWrapper
            ref={(ref) => {
              if (ref) {
                transformRef.current = {
                  zoomIn: () => ref.zoomIn(),
                  zoomOut: () => ref.zoomOut(),
                  resetTransform: () => ref.resetTransform(),
                };
              }
            }}
            initialScale={1}
            minScale={0.5}
            maxScale={5}
            centerOnInit
            wheel={{ step: 0.1 }}
            doubleClick={{ mode: 'toggle', step: 2 }}
          >
            <TransformComponent
              wrapperClass="!w-full !h-full"
              contentClass={cn(
                'flex items-center justify-center',
                'min-w-full min-h-full'
              )}
            >
              <img
                src={imageUrl}
                alt={alt}
                className="max-w-full max-h-full object-contain select-none"
                draggable={false}
              />
            </TransformComponent>
            <ZoomControls />
          </TransformWrapper>

          {/* Keyboard hint */}
          <div className="absolute top-2 right-2 text-xs text-muted-foreground bg-background/80 backdrop-blur-sm rounded px-2 py-1">
            +/- to zoom, 0 to reset
          </div>
        </div>
      </DialogContent>
    </Dialog>
  );
}
