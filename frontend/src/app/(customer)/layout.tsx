// src/app/(customer)/layout.tsx
// Shared layout for all authenticated customer pages

import { UserButton } from "@clerk/nextjs";
import { auth } from "@clerk/nextjs/server";
import { redirect } from "next/navigation";
import Link from "next/link";

const navLinks = [
  { href: "/dashboard", label: "Dashboard", icon: "🏠" },
  { href: "/apply", label: "Apply", icon: "📋" },
  { href: "/kyc", label: "KYC", icon: "🪪" },
  { href: "/notifications", label: "Notifications", icon: "🔔" },
  { href: "/profile", label: "Profile", icon: "👤" },
];

export default async function CustomerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { userId } = await auth();
  if (!userId) redirect("/sign-in");

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {/* Sidebar */}
      <aside className="w-60 bg-white border-r border-gray-200 flex flex-col py-6 px-4 fixed h-full">
        {/* Logo */}
        <div className="flex items-center gap-3 px-2 mb-8">
          <div className="w-9 h-9 bg-blue-900 rounded-lg flex items-center justify-center">
            <span className="text-white font-black text-sm">IB</span>
          </div>
          <span className="font-bold text-gray-900 text-lg">IncedoBank</span>
        </div>

        {/* Nav Links */}
        <nav className="flex-1 space-y-1">
          {navLinks.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-gray-600 hover:bg-blue-50 hover:text-blue-700 transition-colors text-sm font-medium group"
            >
              <span className="text-base">{link.icon}</span>
              {link.label}
            </Link>
          ))}
        </nav>

        {/* User Button */}
        <div className="border-t border-gray-200 pt-4 px-2">
          <UserButton
            appearance={{
              elements: {
                userButtonBox: "flex items-center gap-3",
                userButtonOuterIdentifier: "text-sm font-medium text-gray-700",
              },
            }}
            showName
          />
        </div>
      </aside>

      {/* Main Content */}
      <main className="ml-60 flex-1 p-8">{children}</main>
    </div>
  );
}
