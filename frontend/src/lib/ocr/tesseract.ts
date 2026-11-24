// src/lib/ocr/tesseract.ts
// Client-side OCR using Tesseract.js (runs entirely in browser — free)

import Tesseract from "tesseract.js";

export interface OCRResult {
  text: string;
  confidence: number;
  extractedFields: {
    name?: string;
    dob?: string;
    panNumber?: string;
    aadhaarNumber?: string;
  };
}

/**
 * Run OCR on an image file using Tesseract.js (browser-side, no API call)
 */
export async function runOCR(
  imageFile: File,
  onProgress?: (progress: number) => void
): Promise<OCRResult> {
  const result = await Tesseract.recognize(imageFile, "eng", {
    logger: (m) => {
      if (m.status === "recognizing text" && onProgress) {
        onProgress(Math.round(m.progress * 100));
      }
    },
  });

  const text = result.data.text;
  const confidence = result.data.confidence;

  return {
    text,
    confidence,
    extractedFields: extractFieldsFromText(text),
  };
}

/**
 * Extract structured fields from raw OCR text using regex patterns
 */
function extractFieldsFromText(text: string): OCRResult["extractedFields"] {
  const fields: OCRResult["extractedFields"] = {};

  // PAN Number: 5 uppercase letters, 4 digits, 1 uppercase letter
  const panMatch = text.match(/[A-Z]{5}[0-9]{4}[A-Z]/);
  if (panMatch) fields.panNumber = panMatch[0];

  // Aadhaar: 12 digits (formatted as XXXX XXXX XXXX)
  const aadhaarMatch = text.match(/\d{4}\s\d{4}\s\d{4}/);
  if (aadhaarMatch) fields.aadhaarNumber = aadhaarMatch[0].replace(/\s/g, "");

  // Date of Birth: common formats
  const dobMatch = text.match(/\b(\d{2}[\/\-]\d{2}[\/\-]\d{4})\b/);
  if (dobMatch) fields.dob = dobMatch[0];

  // Name: Look for "Name:" label followed by text
  const nameMatch = text.match(/(?:Name|नाम)[:\s]+([A-Z][a-z]+(?:\s[A-Z][a-z]+)+)/);
  if (nameMatch) fields.name = nameMatch[1];

  return fields;
}

/**
 * Validate document image quality before upload
 * Returns true if image is good enough for OCR
 */
export async function validateImageQuality(file: File): Promise<{
  valid: boolean;
  issues: string[];
}> {
  const issues: string[] = [];

  // Check file size (too small = likely blurry)
  if (file.size < 50_000) {
    issues.push("Image appears too small. Please use a higher resolution image.");
  }

  // Check file size (too large = acceptable, just warn)
  if (file.size > 10_000_000) {
    issues.push("Image is very large. Upload may be slow.");
  }

  return {
    valid: issues.filter((i) => i.includes("small")).length === 0,
    issues,
  };
}
