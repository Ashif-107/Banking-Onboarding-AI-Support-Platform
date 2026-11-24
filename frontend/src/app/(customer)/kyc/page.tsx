// src/app/(customer)/kyc/page.tsx
"use client";

import { useState, useRef } from "react";
import { runOCR, validateImageQuality } from "@/lib/ocr/tesseract";

type DocType = "aadhaar" | "pan" | "selfie" | "payslip" | "passport";

interface UploadedDoc {
  type: DocType;
  file: File;
  ocrText?: string;
  ocrConfidence?: number;
  extractedFields?: Record<string, string>;
  status: "uploading" | "ocr" | "done" | "error";
}

const DOC_CONFIG: Record<DocType, { label: string; icon: string; desc: string; required: boolean }> = {
  aadhaar:  { label: "Aadhaar Card",  icon: "🪪", desc: "Front side of your Aadhaar card", required: true },
  pan:      { label: "PAN Card",      icon: "💳", desc: "Your PAN card (income tax identity)", required: true },
  selfie:   { label: "Live Selfie",   icon: "🤳", desc: "A clear selfie for liveness check", required: true },
  payslip:  { label: "Payslip",       icon: "📄", desc: "Latest 3-month salary slip", required: false },
  passport: { label: "Passport",      icon: "🛂", desc: "Passport (alternative to Aadhaar)", required: false },
};

export default function KYCPage() {
  const [docs, setDocs] = useState<Partial<Record<DocType, UploadedDoc>>>({});
  const [ocrProgress, setOcrProgress] = useState<Partial<Record<DocType, number>>>({});
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [activeDocType, setActiveDocType] = useState<DocType | null>(null);

  const handleFileSelect = async (docType: DocType, file: File) => {
    // Validate image quality
    const quality = await validateImageQuality(file);
    if (!quality.valid) {
      alert(quality.issues.join("\n"));
      return;
    }

    // Set uploading state
    setDocs((prev) => ({
      ...prev,
      [docType]: { type: docType, file, status: "ocr" },
    }));

    // Run OCR (browser-side, free)
    if (docType !== "selfie") {
      try {
        const result = await runOCR(file, (progress) => {
          setOcrProgress((prev) => ({ ...prev, [docType]: progress }));
        });

        setDocs((prev) => ({
          ...prev,
          [docType]: {
            type: docType,
            file,
            ocrText: result.text,
            ocrConfidence: result.confidence,
            extractedFields: result.extractedFields as Record<string, string>,
            status: "done",
          },
        }));
        setOcrProgress((prev) => ({ ...prev, [docType]: 100 }));
      } catch {
        setDocs((prev) => ({
          ...prev,
          [docType]: { type: docType, file, status: "error" },
        }));
      }
    } else {
      // For selfie — just mark done (face-api.js check handled separately)
      setDocs((prev) => ({
        ...prev,
        [docType]: { type: docType, file, status: "done" },
      }));
    }
  };

  const triggerUpload = (docType: DocType) => {
    setActiveDocType(docType);
    fileInputRef.current?.click();
  };

  const requiredDocs = Object.entries(DOC_CONFIG).filter(([, v]) => v.required) as [DocType, typeof DOC_CONFIG[DocType]][];
  const optionalDocs = Object.entries(DOC_CONFIG).filter(([, v]) => !v.required) as [DocType, typeof DOC_CONFIG[DocType]][];
  const completedRequired = requiredDocs.filter(([k]) => docs[k]?.status === "done").length;

  return (
    <div className="max-w-3xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">KYC Verification</h1>
        <p className="text-gray-500 text-sm mt-1">
          Upload your documents. OCR runs in your browser — nothing is processed on external servers.
        </p>
      </div>

      {/* Progress Bar */}
      <div className="bg-white rounded-xl border border-gray-200 p-5 mb-6">
        <div className="flex items-center justify-between mb-3">
          <span className="text-sm font-medium text-gray-700">Required documents</span>
          <span className="text-sm font-bold text-blue-600">
            {completedRequired} / {requiredDocs.length} complete
          </span>
        </div>
        <div className="w-full bg-gray-100 rounded-full h-2">
          <div
            className="bg-blue-600 h-2 rounded-full transition-all duration-500"
            style={{ width: `${(completedRequired / requiredDocs.length) * 100}%` }}
          />
        </div>
      </div>

      {/* Required Docs */}
      <h2 className="font-semibold text-gray-900 mb-3">Required</h2>
      <div className="space-y-3 mb-6">
        {requiredDocs.map(([docType, config]) => (
          <DocUploadCard
            key={docType}
            docType={docType as DocType}
            config={config}
            uploaded={docs[docType as DocType]}
            ocrProgress={ocrProgress[docType as DocType]}
            onUpload={() => triggerUpload(docType as DocType)}
          />
        ))}
      </div>

      {/* Optional Docs */}
      <h2 className="font-semibold text-gray-900 mb-3">Optional</h2>
      <div className="space-y-3 mb-8">
        {optionalDocs.map(([docType, config]) => (
          <DocUploadCard
            key={docType}
            docType={docType as DocType}
            config={config}
            uploaded={docs[docType as DocType]}
            ocrProgress={ocrProgress[docType as DocType]}
            onUpload={() => triggerUpload(docType as DocType)}
          />
        ))}
      </div>

      {/* Submit */}
      <button
        disabled={completedRequired < requiredDocs.length}
        className="w-full bg-blue-700 text-white font-bold py-3 rounded-xl hover:bg-blue-800 transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
      >
        Submit KYC Documents
      </button>

      {/* Hidden file input */}
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*,.pdf"
        className="hidden"
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (file && activeDocType) handleFileSelect(activeDocType, file);
          e.target.value = "";
        }}
      />
    </div>
  );
}

function DocUploadCard({
  docType,
  config,
  uploaded,
  ocrProgress,
  onUpload,
}: {
  docType: DocType;
  config: typeof DOC_CONFIG[DocType];
  uploaded?: UploadedDoc;
  ocrProgress?: number;
  onUpload: () => void;
}) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 p-4 flex items-center gap-4">
      <div className="text-3xl w-12 text-center">{config.icon}</div>
      <div className="flex-1">
        <div className="flex items-center gap-2 mb-0.5">
          <p className="font-medium text-gray-900 text-sm">{config.label}</p>
          {uploaded?.status === "done" && (
            <span className="text-green-500 text-xs font-medium">✓ Verified</span>
          )}
          {uploaded?.status === "error" && (
            <span className="text-red-500 text-xs font-medium">✗ Failed</span>
          )}
        </div>
        <p className="text-xs text-gray-500">{config.desc}</p>

        {/* OCR Progress */}
        {uploaded?.status === "ocr" && (
          <div className="mt-2">
            <div className="text-xs text-blue-600 mb-1">
              Running OCR... {ocrProgress ?? 0}%
            </div>
            <div className="w-full bg-gray-100 rounded-full h-1.5">
              <div
                className="bg-blue-500 h-1.5 rounded-full transition-all"
                style={{ width: `${ocrProgress ?? 0}%` }}
              />
            </div>
          </div>
        )}

        {/* Extracted Fields Preview */}
        {uploaded?.extractedFields && Object.keys(uploaded.extractedFields).length > 0 && (
          <div className="mt-2 flex flex-wrap gap-2">
            {Object.entries(uploaded.extractedFields).map(([k, v]) => (
              <span key={k} className="bg-green-50 border border-green-200 text-green-800 text-xs px-2 py-0.5 rounded">
                {k}: {v}
              </span>
            ))}
          </div>
        )}
      </div>

      <button
        onClick={onUpload}
        className="text-sm font-medium text-blue-600 hover:text-blue-800 border border-blue-200 hover:border-blue-400 px-3 py-1.5 rounded-lg transition-all"
      >
        {uploaded ? "Re-upload" : "Upload"}
      </button>
    </div>
  );
}
