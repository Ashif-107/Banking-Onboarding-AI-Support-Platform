// src/app/(agent)/layout.tsx
// Agent Cockpit layout

import { UserButton } from "@clerk/nextjs";
import { auth } from "@clerk/nextjs/server";
import { redirect } from "next/navigation";
import Link from "next/link";

const agentNav = [
  { href: "/agent/cockpit", label: "Cockpit", icon: "🎯" },
  { href: "/agent/tickets", label: "Tickets", icon: "🎫" },
  { href: "/agent/analytics", label: "Analytics", icon: "📊" },
];

export default async function AgentLayout({ children }: { children: React.ReactNode }) {
  const { userId } = await auth();
  if (!userId) redirect("/sign-in");

  return (
    <div className="min-h-screen bg-slate-900 text-white flex">
      {/* Sidebar */}
      <aside className="w-56 bg-slate-800 border-r border-slate-700 flex flex-col py-6 px-4 fixed h-full">
        <div className="flex items-center gap-2 px-2 mb-8">
          <div className="w-8 h-8 bg-blue-500 rounded-lg flex items-center justify-center">
            <span className="font-black text-xs">AI</span>
          </div>
          <span className="font-bold text-sm">Agent Cockpit</span>
        </div>
        <nav className="flex-1 space-y-1">
          {agentNav.map((link) => (
            <Link
              key={link.href}
              href={link.href}
              className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-slate-400 hover:bg-slate-700 hover:text-white transition-colors text-sm font-medium"
            >
              <span>{link.icon}</span>
              {link.label}
            </Link>
          ))}
        </nav>
        <div className="border-t border-slate-700 pt-4 px-2">
          <UserButton showName appearance={{ elements: { userButtonOuterIdentifier: "text-xs text-slate-400" } }} />
        </div>
      </aside>
      <main className="ml-56 flex-1 p-6">{children}</main>
    </div>
  );
}
