"use client"

import { useCallback, useState } from 'react'
import { useDropzone } from 'react-dropzone'
import { toast } from 'sonner'
import { Upload, X, FileImage, Loader2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { Progress } from '@/components/ui/progress'
import { useUploadReceipts } from '@/hooks/queries/use-receipts'

const ACCEPTED_FILE_TYPES = {
  'image/jpeg': ['.jpg', '.jpeg'],
  'image/png': ['.png'],
  'image/webp': ['.webp'],
  'image/heic': ['.heic'],
  'application/pdf': ['.pdf'],
}

const MAX_FILE_SIZE = 10 * 1024 * 1024 // 10MB
const MAX_FILES = 20

interface ReceiptUploadDropzoneProps {
  onUploadComplete?: () => void
}

export function ReceiptUploadDropzone({ onUploadComplete }: ReceiptUploadDropzoneProps) {
  const [files, setFiles] = useState<File[]>([])
  const [uploadProgress, setUploadProgress] = useState(0)
  const { mutate: uploadReceipts, isPending } = useUploadReceipts()

  const onDrop = useCallback((acceptedFiles: File[], rejectedFiles: unknown[]) => {
    if (rejectedFiles.length > 0) {
      toast.error('Some files were rejected. Please check file type and size.')
    }

    setFiles(prev => {
      const newFiles = [...prev, ...acceptedFiles]
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
          or click to browse. Supports JPG, PNG, WebP, HEIC, PDF (max 10MB each)
        </p>
      </div>

      {files.length > 0 && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-medium">
              {files.length} file{files.length !== 1 ? 's' : ''} selected
            </h3>
            <Button
              variant="ghost"
              size="sm"
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
                  className="h-8 w-8"
                  onClick={() => removeFile(index)}
                  disabled={isPending}
                >
                  <X className="h-4 w-4" />
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
