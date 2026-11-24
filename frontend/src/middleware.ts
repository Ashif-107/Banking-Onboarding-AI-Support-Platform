// src/middleware.ts
import { clerkMiddleware, createRouteMatcher } from "@clerk/nextjs/server";

// Define protected routes
const isCustomerRoute = createRouteMatcher([
  "/dashboard(.*)",
  "/apply(.*)",
  "/kyc(.*)",
  "/notifications(.*)",
  "/profile(.*)",
  "/onboarding(.*)",
]);

const isAgentRoute = createRouteMatcher([
  "/agent(.*)",
]);

const isAdminRoute = createRouteMatcher([
  "/admin(.*)",
]);

export default clerkMiddleware(async (auth, req) => {
  // Protect all customer, agent, and admin routes
  if (
    isCustomerRoute(req) ||
    isAgentRoute(req) ||
    isAdminRoute(req)
  ) {
    await auth.protect();
  }
});

export const config = {
  matcher: [
    // Skip Next.js internals and static files
    "/((?!_next|[^?]*\\.(?:html?|css|js(?!on)|jpe?g|webp|png|gif|svg|ttf|woff2?|ico|csv|docx?|xlsx?|zip|webmanifest)).*)",
    // Always run for API routes
    "/(api|trpc)(.*)",
  ],
};
