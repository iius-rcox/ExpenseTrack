'use client'

/**
 * CameraCapture Component (T097)
 *
 * Mobile-optimized camera capture for receipt photos.
 * Uses device camera (rear-facing by default) for direct capture.
 * Includes preview, retake, and quality optimization.
 */

import * as React from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import {
  Camera,
  X,
  RotateCcw,
  Check,
  FlipHorizontal,
  Loader2,
  AlertCircle,
  Image as ImageIcon,
} from 'lucide-react'

export interface CameraCaptureProps {
  /** Called when a photo is captured and confirmed */
  onCapture: (file: File, previewUrl: string) => void
  /** Called when user cancels capture */
  onCancel: () => void
  /** Maximum image dimension (width or height) */
  maxDimension?: number
  /** JPEG quality (0-1) for compression */
  quality?: number
  /** Whether to show the capture UI in a modal */
  isModal?: boolean
  /** Additional CSS classes */
  className?: string
}

type CaptureState = 'idle' | 'streaming' | 'captured' | 'error'

const DEFAULT_MAX_DIMENSION = 2048
const DEFAULT_QUALITY = 0.85

/**
 * CameraCapture - Receipt photo capture for mobile
 *
 * Features:
 * - Direct camera access using getUserMedia
 * - Rear camera (environment) by default
 * - Front/rear camera toggle
 * - Preview before confirming
 * - Image optimization (resize + compress)
 * - Fallback to file picker if camera unavailable
 */
export function CameraCapture({
  onCapture,
  onCancel,
  maxDimension = DEFAULT_MAX_DIMENSION,
  quality = DEFAULT_QUALITY,
  isModal = true,
  className,
}: CameraCaptureProps) {
  const videoRef = React.useRef<HTMLVideoElement>(null)
  const canvasRef = React.useRef<HTMLCanvasElement>(null)
  const streamRef = React.useRef<MediaStream | null>(null)
  const fileInputRef = React.useRef<HTMLInputElement>(null)

  const [state, setState] = React.useState<CaptureState>('idle')
  const [facingMode, setFacingMode] = React.useState<'environment' | 'user'>('environment')
  const [capturedImage, setCapturedImage] = React.useState<string | null>(null)
  const [error, setError] = React.useState<string | null>(null)
  const [isProcessing, setIsProcessing] = React.useState(false)

  // Start camera stream
  const startCamera = React.useCallback(async () => {
    try {
      setError(null)
      setState('idle')

      // Stop any existing stream
      if (streamRef.current) {
        streamRef.current.getTracks().forEach((track) => track.stop())
      }

      const constraints: MediaStreamConstraints = {
        video: {
          facingMode: { ideal: facingMode },
          width: { ideal: maxDimension },
          height: { ideal: maxDimension },
        },
        audio: false,
      }

      const stream = await navigator.mediaDevices.getUserMedia(constraints)
      streamRef.current = stream

      if (videoRef.current) {
        videoRef.current.srcObject = stream
        await videoRef.current.play()
        setState('streaming')
      }
    } catch (err) {
      console.error('Camera access error:', err)

      if (err instanceof DOMException) {
        if (err.name === 'NotAllowedError') {
          setError('Camera access denied. Please allow camera access in your browser settings.')
        } else if (err.name === 'NotFoundError') {
          setError('No camera found on this device.')
        } else {
          setError('Unable to access camera. You can still upload from your gallery.')
        }
      } else {
        setError('Camera error. You can still upload from your gallery.')
      }

      setState('error')
    }
  }, [facingMode, maxDimension])

  // Stop camera stream
  const stopCamera = React.useCallback(() => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop())
      streamRef.current = null
    }
    if (videoRef.current) {
      videoRef.current.srcObject = null
    }
  }, [])

  // Initialize camera on mount
  React.useEffect(() => {
    startCamera()
    return stopCamera
  }, [startCamera, stopCamera])

  // Toggle front/rear camera
  const toggleCamera = () => {
    setFacingMode((prev) => (prev === 'environment' ? 'user' : 'environment'))
  }

  // Restart camera when facing mode changes
  React.useEffect(() => {
    if (state === 'streaming') {
      startCamera()
    }
  }, [facingMode]) // eslint-disable-line react-hooks/exhaustive-deps

  // Capture photo from video stream
  const capturePhoto = () => {
    if (!videoRef.current || !canvasRef.current) return

    const video = videoRef.current
    const canvas = canvasRef.current
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    // Set canvas size to video dimensions (or max dimension)
    const videoWidth = video.videoWidth
    const videoHeight = video.videoHeight
    const scale = Math.min(1, maxDimension / Math.max(videoWidth, videoHeight))

    canvas.width = videoWidth * scale
    canvas.height = videoHeight * scale

    // Draw video frame to canvas
    ctx.drawImage(video, 0, 0, canvas.width, canvas.height)

    // Get data URL for preview
    const dataUrl = canvas.toDataURL('image/jpeg', quality)
    setCapturedImage(dataUrl)
    setState('captured')

    // Stop camera stream while previewing
    stopCamera()
  }

  // Retake photo
  const retakePhoto = () => {
    setCapturedImage(null)
    setState('idle')
    startCamera()
  }

  // Confirm and process captured photo
  const confirmPhoto = async () => {
    if (!capturedImage || !canvasRef.current) return

    setIsProcessing(true)

    try {
      // Convert canvas to blob
      const blob = await new Promise<Blob>((resolve, reject) => {
        canvasRef.current!.toBlob(
          (blob) => {
            if (blob) resolve(blob)
            else reject(new Error('Failed to create image blob'))
          },
          'image/jpeg',
          quality
        )
      })

      // Create file from blob
      const file = new File([blob], `receipt-${Date.now()}.jpg`, {
        type: 'image/jpeg',
      })

      onCapture(file, capturedImage)
    } catch (err) {
      console.error('Error processing image:', err)
      setError('Failed to process image. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }

  // Handle file input (fallback or gallery)
  const handleFileSelect = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0]
    if (!file) return

    setIsProcessing(true)

    try {
      // Resize if needed
      const optimizedFile = await optimizeImage(file, maxDimension, quality)
      const previewUrl = URL.createObjectURL(optimizedFile)

      onCapture(optimizedFile, previewUrl)
    } catch (err) {
      console.error('Error processing image:', err)
      setError('Failed to process image. Please try again.')
    } finally {
      setIsProcessing(false)
    }
  }

  // Open file picker for gallery
  const openGallery = () => {
    fileInputRef.current?.click()
  }

  const content = (
    <div
      className={cn(
        'relative flex flex-col bg-black',
        isModal && 'fixed inset-0 z-50',
        className
      )}
    >
      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        capture="environment"
        onChange={handleFileSelect}
        className="hidden"
      />

      {/* Hidden canvas for processing */}
      <canvas ref={canvasRef} className="hidden" />

      {/* Header */}
      <div className="flex items-center justify-between p-4 bg-black/80 backdrop-blur">
        <Button
          variant="ghost"
          size="icon"
          onClick={onCancel}
          className="text-white hover:bg-white/10"
        >
          <X className="h-6 w-6" />
        </Button>

        <h2 className="text-white font-medium">Capture Receipt</h2>

        {state === 'streaming' && (
          <Button
            variant="ghost"
            size="icon"
            onClick={toggleCamera}
            className="text-white hover:bg-white/10"
          >
            <FlipHorizontal className="h-5 w-5" />
          </Button>
        )}
        {state !== 'streaming' && <div className="w-10" />}
      </div>

      {/* Main content area */}
      <div className="flex-1 relative overflow-hidden">
        <AnimatePresence mode="wait">
          {/* Camera streaming */}
          {(state === 'idle' || state === 'streaming') && (
            <motion.div
              key="streaming"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 flex items-center justify-center"
            >
              <video
                ref={videoRef}
                autoPlay
                playsInline
                muted
                className="h-full w-full object-cover"
              />

              {/* Capture frame guide */}
              <div className="absolute inset-4 border-2 border-white/30 rounded-lg pointer-events-none">
                <div className="absolute top-0 left-0 w-8 h-8 border-t-4 border-l-4 border-white rounded-tl-lg" />
                <div className="absolute top-0 right-0 w-8 h-8 border-t-4 border-r-4 border-white rounded-tr-lg" />
                <div className="absolute bottom-0 left-0 w-8 h-8 border-b-4 border-l-4 border-white rounded-bl-lg" />
                <div className="absolute bottom-0 right-0 w-8 h-8 border-b-4 border-r-4 border-white rounded-br-lg" />
              </div>

              {/* Loading indicator */}
              {state === 'idle' && (
                <div className="absolute inset-0 flex items-center justify-center bg-black/50">
                  <Loader2 className="h-8 w-8 text-white animate-spin" />
                </div>
              )}
            </motion.div>
          )}

          {/* Preview captured image */}
          {state === 'captured' && capturedImage && (
            <motion.div
              key="preview"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 flex items-center justify-center"
            >
              <img
                src={capturedImage}
                alt="Captured receipt"
                className="h-full w-full object-contain"
              />
            </motion.div>
          )}

          {/* Error state */}
          {state === 'error' && (
            <motion.div
              key="error"
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              className="absolute inset-0 flex flex-col items-center justify-center p-6 text-center"
            >
              <AlertCircle className="h-12 w-12 text-destructive mb-4" />
              <p className="text-white text-lg mb-6">{error}</p>
              <Button onClick={openGallery} variant="secondary" size="lg">
                <ImageIcon className="h-5 w-5 mr-2" />
                Choose from Gallery
              </Button>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Controls */}
      <div className="p-4 bg-black/80 backdrop-blur">
        <AnimatePresence mode="wait">
          {/* Capture controls */}
          {state === 'streaming' && (
            <motion.div
              key="capture-controls"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              className="flex items-center justify-center gap-6"
            >
              <Button
                variant="ghost"
                size="icon"
                onClick={openGallery}
                className="text-white hover:bg-white/10 h-12 w-12"
              >
                <ImageIcon className="h-6 w-6" />
              </Button>

              <Button
                onClick={capturePhoto}
                size="icon"
                className={cn(
                  'h-16 w-16 rounded-full',
                  'bg-white hover:bg-white/90',
                  'border-4 border-white/50'
                )}
              >
                <Camera className="h-7 w-7 text-black" />
              </Button>

              <div className="w-12" /> {/* Spacer for balance */}
            </motion.div>
          )}

          {/* Preview controls */}
          {state === 'captured' && (
            <motion.div
              key="preview-controls"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              className="flex items-center justify-center gap-6"
            >
              <Button
                variant="outline"
                size="lg"
                onClick={retakePhoto}
                disabled={isProcessing}
                className="text-white border-white/30 hover:bg-white/10"
              >
                <RotateCcw className="h-5 w-5 mr-2" />
                Retake
              </Button>

              <Button
                size="lg"
                onClick={confirmPhoto}
                disabled={isProcessing}
                className="bg-white text-black hover:bg-white/90 min-w-[120px]"
              >
                {isProcessing ? (
                  <Loader2 className="h-5 w-5 animate-spin" />
                ) : (
                  <>
                    <Check className="h-5 w-5 mr-2" />
                    Use Photo
                  </>
                )}
              </Button>
            </motion.div>
          )}

          {/* Error controls */}
          {state === 'error' && (
            <motion.div
              key="error-controls"
              initial={{ opacity: 0, y: 20 }}
              animate={{ opacity: 1, y: 0 }}
              exit={{ opacity: 0, y: -20 }}
              className="flex items-center justify-center gap-4"
            >
              <Button
                variant="outline"
                onClick={startCamera}
                className="text-white border-white/30 hover:bg-white/10"
              >
                <RotateCcw className="h-5 w-5 mr-2" />
                Try Again
              </Button>
            </motion.div>
          )}
        </AnimatePresence>
      </div>

      {/* Processing overlay */}
      {isProcessing && (
        <div className="absolute inset-0 bg-black/50 flex items-center justify-center z-10">
          <div className="text-center text-white">
            <Loader2 className="h-10 w-10 animate-spin mx-auto mb-3" />
            <p>Processing...</p>
          </div>
        </div>
      )}
    </div>
  )

  return content
}

/**
 * Helper: Optimize image (resize and compress)
 */
async function optimizeImage(
  file: File,
  maxDimension: number,
  quality: number
): Promise<File> {
  return new Promise((resolve, reject) => {
    const img = new Image()
    const canvas = document.createElement('canvas')
    const ctx = canvas.getContext('2d')

    img.onload = () => {
      if (!ctx) {
        reject(new Error('Failed to get canvas context'))
        return
      }

      // Calculate new dimensions
      let { width, height } = img
      const scale = Math.min(1, maxDimension / Math.max(width, height))

      width = Math.round(width * scale)
      height = Math.round(height * scale)

      // Set canvas size and draw
      canvas.width = width
      canvas.height = height
      ctx.drawImage(img, 0, 0, width, height)

      // Convert to blob
      canvas.toBlob(
        (blob) => {
          if (blob) {
            const optimizedFile = new File(
              [blob],
              file.name.replace(/\.[^.]+$/, '.jpg'),
              { type: 'image/jpeg' }
            )
            resolve(optimizedFile)
          } else {
            reject(new Error('Failed to create blob'))
          }
        },
        'image/jpeg',
        quality
      )
    }

    img.onerror = () => reject(new Error('Failed to load image'))

    // Load image from file
    const reader = new FileReader()
    reader.onload = (e) => {
      img.src = e.target?.result as string
    }
    reader.onerror = () => reject(new Error('Failed to read file'))
    reader.readAsDataURL(file)
  })
}

/**
 * CameraCaptureButton - Simple button that opens camera capture modal
 */
export function CameraCaptureButton({
  onCapture,
  disabled,
  className,
}: {
  onCapture: (file: File, previewUrl: string) => void
  disabled?: boolean
  className?: string
}) {
  const [isOpen, setIsOpen] = React.useState(false)

  const handleCapture = (file: File, previewUrl: string) => {
    setIsOpen(false)
    onCapture(file, previewUrl)
  }

  return (
    <>
      <Button
        onClick={() => setIsOpen(true)}
        disabled={disabled}
        variant="outline"
        className={cn('gap-2', className)}
      >
        <Camera className="h-4 w-4" />
        Take Photo
      </Button>

      <AnimatePresence>
        {isOpen && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
          >
            <CameraCapture
              onCapture={handleCapture}
              onCancel={() => setIsOpen(false)}
              isModal
            />
          </motion.div>
        )}
      </AnimatePresence>
    </>
  )
}

export default CameraCapture
