"use client"

import { useCallback, useState, useEffect } from 'react'
import { useDropzone } from 'react-dropzone'
import { toast } from 'sonner'
import { Upload, X, FileImage, Loader2, FolderOpen } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { useUploadReceipts } from '@/hooks/queries/use-receipts'
import { CameraCaptureButton } from '@/components/mobile/camera-capture'

const ACCEPTED_FILE_TYPES = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/webp': ['.webp'],
  'image/heic': ['.heic'],
  'application/pdf': ['.pdf'],
  'text/html': ['.html', '.htm'],
  // Additional HTML-related MIME types that browsers may report for saved emails/web pages
  'application/xhtml+xml': ['.xhtml'],
  'application/x-html': ['.html', '.htm'],
}

const MAX_FILE_SIZE = 20 * 1024 * 1024 // 20MB per spec
const MAX_FILES = 20

interface ReceiptUploadDropzoneProps {
  onUploadComplete?: () => void
}

export function ReceiptUploadDropzone({ onUploadComplete }: ReceiptUploadDropzoneProps) {
  const [files, setFiles] = useState<File[]>([])
  const [uploadProgress, setUploadProgress] = useState(0)
  const [isMobile, setIsMobile] = useState(false)
  const { mutate: uploadReceipts, isPending } = useUploadReceipts()

  // Detect mobile viewport
  useEffect(() => {
    const checkMobile = () => setIsMobile(window.innerWidth < 768)
    checkMobile()
    window.addEventListener('resize', checkMobile)
    return () => window.removeEventListener('resize', checkMobile)
  }, [])

  const onDrop = useCallback((acceptedFiles: File[], rejectedFiles: File[] | { file: File }[]) => {
    // Rescue HTML files that may have been rejected due to browser reporting wrong MIME type
    const htmlExtensions = ['.html', '.htm', '.xhtml']
    const rescuedFiles: File[] = []
    const trulyRejectedFiles: (File | { file: File })[] = []

    for (const rejected of rejectedFiles) {
      const file = 'file' in rejected ? rejected.file : rejected
      const ext = file.name.toLowerCase().slice(file.name.lastIndexOf('.'))
      if (htmlExtensions.includes(ext) && file.size <= MAX_FILE_SIZE) {
        // Rescue HTML file that was rejected due to MIME type mismatch
        rescuedFiles.push(file)
      } else {
        trulyRejectedFiles.push(rejected)
      }
    }

    if (trulyRejectedFiles.length > 0) {
      toast.error('Some files were rejected. Please check file type and size.')
    }

    const allAcceptedFiles = [...acceptedFiles, ...rescuedFiles]

    setFiles(prev => {
      const newFiles = [...prev, ...allAcceptedFiles]
      if (newFiles.length > MAX_FILES) {
        toast.error(`Maximum ${MAX_FILES} files allowed`)
        return prev
      }
      return newFiles
    })
  }, [])

  const removeFile = (index: number) => {
    setFiles(prev => prev.filter((_, i) => i !== index))
  }

  // Handle camera capture
  const handleCameraCapture = useCallback((file: File) => {
    setFiles(prev => {
      const newFiles = [...prev, file]
      if (newFiles.length > MAX_FILES) {
        toast.error(`Maximum ${MAX_FILES} files allowed`)
        return prev
      }
      return newFiles
    })
    toast.success('Photo captured')
  }, [])

  const handleUpload = () => {
    if (files.length === 0) return

    setUploadProgress(0)
    uploadReceipts(
      {
        files,
        onProgress: (progress) => {
          setUploadProgress(Math.round(progress))
        },
      },
      {
        onSuccess: (response) => {
          const successCount = response.totalUploaded
          const failedCount = response.failed.length

          if (failedCount > 0) {
            toast.warning(`Uploaded ${successCount} receipts, ${failedCount} failed`)
          } else {
            toast.success(`Successfully uploaded ${successCount} receipts`)
          }

          setFiles([])
          setUploadProgress(0)
          onUploadComplete?.()
        },
        onError: (error) => {
          toast.error(`Upload failed: ${error.message}`)
          setUploadProgress(0)
        },
      }
    )
  }

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: ACCEPTED_FILE_TYPES,
    maxSize: MAX_FILE_SIZE,
    maxFiles: MAX_FILES,
    disabled: isPending,
  })

  return (
    <div className="space-y-4">
      {/* Mobile: Show camera and gallery buttons */}
      {isMobile ? (
        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <CameraCaptureButton
              onCapture={handleCameraCapture}
              disabled={isPending}
              className="h-24 flex-col"
            />
            <div
              {...getRootProps()}
              className={cn(
                'flex flex-col items-center justify-center gap-2',
                'h-24 rounded-lg border-2 border-dashed',
                'cursor-pointer transition-colors',
                'border-muted-foreground/25 hover:border-primary/50',
                isPending && 'cursor-not-allowed opacity-50'
              )}
            >
              <input {...getInputProps()} />
              <FolderOpen className="h-8 w-8 text-muted-foreground" />
              <span className="text-sm font-medium">Browse Files</span>
            </div>
          </div>
          <p className="text-xs text-muted-foreground text-center">
            Supports JPG, PNG, WebP, HEIC, PDF, HTML (max 20MB each)
          </p>
        </div>
      ) : (
        /* Desktop: Show drag-and-drop zone */
        <div
          {...getRootProps()}
          className={cn(
            'border-2 border-dashed rounded-lg p-8 text-center cursor-pointer transition-colors',
            isDragActive
              ? 'border-primary bg-primary/5'
              : 'border-muted-foreground/25 hover:border-primary/50',
            isPending && 'cursor-not-allowed opacity-50'
          )}
        >
          <input {...getInputProps()} />
          <Upload className="mx-auto h-12 w-12 text-muted-foreground" />
          <p className="mt-4 text-lg font-medium">
            {isDragActive ? 'Drop files here' : 'Drag & drop receipts here'}
          </p>
          <p className="mt-2 text-sm text-muted-foreground">
            or click to browse. Supports JPG, PNG, WebP, HEIC, PDF, HTML (max 20MB each)
          </p>
        </div>
      )}

      {files.length > 0 && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium">
              {files.length} file{files.length !== 1 ? 's' : ''} selected
            </h3>
            <Button
              variant="ghost"
              size="sm"
              className="min-h-[44px] md:min-h-0"
              onClick={() => setFiles([])}
              disabled={isPending}
            >
              Clear all
            </Button>
          </div>

          <div className="max-h-48 overflow-y-auto space-y-2">
            {files.map((file, index) => (
              <div
                key={`${file.name}-${index}`}
                className="flex items-center gap-3 p-2 rounded-md bg-muted/50"
              >
                <FileImage className="h-5 w-5 text-muted-foreground flex-shrink-0" />
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium truncate">{file.name}</p>
                  <p className="text-xs text-muted-foreground">
                    {(file.size / 1024).toFixed(1)} KB
                  </p>
                </div>
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-11 w-11 md:h-8 md:w-8"
                  onClick={() => removeFile(index)}
                  disabled={isPending}
                >
                  <X className="h-5 w-5 md:h-4 md:w-4" />
                </Button>
              </div>
            ))}
          </div>

          {isPending && (
            <div className="space-y-2">
              <div className="flex items-center justify-between text-sm">
                <span>Uploading...</span>
                <span>{uploadProgress}%</span>
              </div>
              <Progress value={uploadProgress} />
            </div>
          )}

          <Button
            onClick={handleUpload}
            disabled={isPending || files.length === 0}
            className="w-full"
          >
            {isPending ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                Uploading...
              </>
            ) : (
              <>
                <Upload className="mr-2 h-4 w-4" />
                Upload {files.length} Receipt{files.length !== 1 ? 's' : ''}
              </>
            )}
          </Button>
        </div>
      )}
    </div>
  )
}
