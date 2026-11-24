// src/lib/api/client.ts
// Axios client with Clerk JWT auth interceptor

import axios from "axios";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000";

export const apiClient = axios.create({
  baseURL: API_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 30000,
});

// Inject Clerk JWT token into every request
apiClient.interceptors.request.use(async (config) => {
  try {
    // Dynamic import to avoid SSR issues
    const { auth } = await import("@clerk/nextjs/server");
    const { getToken } = auth();
    const token = await getToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
  } catch {
    // Client-side: token injected per-call
  }
  return config;
});

// Global error handler
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Redirect to sign-in
      if (typeof window !== "undefined") {
        window.location.href = "/sign-in";
      }
    }
    return Promise.reject(error);
  }
);

// ---- API modules ----
export const authApi = {
  syncUser: (data: { clerkUserId: string; email: string; phone?: string }) =>
    apiClient.post("/api/users/sync", data),
  getProfile: () => apiClient.get("/api/users/profile"),
  updateProfile: (data: unknown) => apiClient.put("/api/users/profile", data),
};

export const kycApi = {
  getStatus: () => apiClient.get("/api/kyc/status"),
  getUploadUrl: (docType: string, fileName: string, mimeType: string) =>
    apiClient.post("/api/kyc/upload-url", { docType, fileName, mimeType }),
  confirmUpload: (data: {
    docType: string;
    r2ObjectKey: string;
    ocrText?: string;
    faceDetected?: boolean;
  }) => apiClient.post("/api/kyc/documents", data),
};

export const productsApi = {
  getAll: () => apiClient.get("/api/products"),
  getById: (id: string) => apiClient.get(`/api/products/${id}`),
  screenEligibility: (productId: string, profileData: unknown) =>
    apiClient.post(`/api/products/${productId}/screen`, { profileData }),
};

export const applicationsApi = {
  create: (data: { productId: string; formData: unknown }) =>
    apiClient.post("/api/applications", data),
  getAll: () => apiClient.get("/api/applications"),
  getById: (id: string) => apiClient.get(`/api/applications/${id}`),
  submit: (id: string) => apiClient.post(`/api/applications/${id}/submit`),
};

export const notificationsApi = {
  getAll: () => apiClient.get("/api/notifications"),
  markRead: (id: string) => apiClient.patch(`/api/notifications/${id}/read`),
  markAllRead: () => apiClient.patch("/api/notifications/read-all"),
};

export const ticketsApi = {
  create: (data: { subject: string; rawText: string; channel: string }) =>
    apiClient.post("/api/tickets", data),
  getAll: (params?: { status?: string; page?: number }) =>
    apiClient.get("/api/tickets", { params }),
  getById: (id: string) => apiClient.get(`/api/tickets/${id}`),
  summarize: (id: string) => apiClient.post(`/api/tickets/${id}/summarize`),
  getSuggestedResponses: (id: string) =>
    apiClient.get(`/api/tickets/${id}/responses/suggest`),
  sendResponse: (id: string, data: { finalResponse: string; tone: string; aiDraft?: string }) =>
    apiClient.post(`/api/tickets/${id}/responses`, data),
  getNBA: (id: string) => apiClient.get(`/api/tickets/${id}/nba`),
  logNBAAction: (id: string, data: { actionType: string; actionData?: unknown }) =>
    apiClient.post(`/api/tickets/${id}/nba/log`, data),
};
