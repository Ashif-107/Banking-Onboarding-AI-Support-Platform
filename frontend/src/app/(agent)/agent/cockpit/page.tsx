// src/app/(agent)/agent/cockpit/page.tsx
// AI Agent Cockpit — main dashboard

import Link from "next/link";

export default function CockpitPage() {
  return (
    <div className="max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-xl font-bold text-white">AI Agent Cockpit</h1>
          <p className="text-slate-400 text-sm mt-0.5">
            AI-powered tools to resolve customer issues faster
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
          <span className="text-green-400 text-sm font-medium">Groq AI Connected</span>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4 mb-8">
        {[
          { label: "Open Tickets", value: "0", icon: "🎫", trend: "" },
          { label: "Avg Resolution", value: "—", icon: "⏱️", trend: "" },
          { label: "CSAT Score", value: "—", icon: "⭐", trend: "" },
          { label: "NBA Adoption", value: "0%", icon: "🎯", trend: "" },
        ].map((stat) => (
          <div key={stat.label} className="bg-slate-800 rounded-xl border border-slate-700 p-4">
            <div className="text-2xl mb-2">{stat.icon}</div>
            <div className="text-2xl font-bold text-white">{stat.value}</div>
            <div className="text-slate-400 text-xs">{stat.label}</div>
          </div>
        ))}
      </div>

      {/* AI Features Grid */}
      <div className="grid md:grid-cols-2 gap-5 mb-6">
        {/* Ticket Queue */}
        <div className="bg-slate-800 rounded-2xl border border-slate-700 p-5">
          <div className="flex items-center justify-between mb-4">
            <h2 className="font-semibold text-white">🎫 Ticket Queue</h2>
            <Link href="/agent/tickets" className="text-blue-400 text-xs hover:text-blue-300">
              View all →
            </Link>
          </div>
          <div className="text-center py-8 text-slate-500">
            <div className="text-4xl mb-2">🎉</div>
            <p className="text-sm">No open tickets. All clear!</p>
          </div>
        </div>

        {/* AI Capabilities */}
        <div className="bg-slate-800 rounded-2xl border border-slate-700 p-5">
          <h2 className="font-semibold text-white mb-4">🤖 AI Capabilities</h2>
          <div className="space-y-3">
            {[
              {
                icon: "📝",
                title: "Ticket Summarization",
                desc: "Groq Llama 3.3 70B summarizes tickets in <2s",
                badge: "Active",
              },
              {
                icon: "✍️",
                title: "Response Generation",
                desc: "3-tone AI drafts with policy grounding",
                badge: "Active",
              },
              {
                icon: "😤",
                title: "Sentiment Analysis",
                desc: "HuggingFace RoBERTa emotion detection",
                badge: "Active",
              },
              {
                icon: "🎯",
                title: "Next-Best Actions",
                desc: "Rule engine + ML-ranked action cards",
                badge: "Active",
              },
            ].map((cap) => (
              <div key={cap.title} className="flex items-start gap-3 p-3 bg-slate-700/50 rounded-lg">
                <span className="text-xl">{cap.icon}</span>
                <div className="flex-1">
                  <div className="flex items-center gap-2">
                    <p className="text-sm font-medium text-white">{cap.title}</p>
                    <span className="text-xs bg-green-900/50 text-green-400 px-1.5 py-0.5 rounded">
                      {cap.badge}
                    </span>
                  </div>
                  <p className="text-xs text-slate-400 mt-0.5">{cap.desc}</p>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Churn Risk Alert */}
      <div className="bg-red-900/30 border border-red-700/50 rounded-2xl p-5">
        <div className="flex items-center gap-3 mb-2">
          <span className="text-2xl">🚨</span>
          <h2 className="font-semibold text-red-300">High Churn Risk Customers</h2>
        </div>
        <p className="text-slate-400 text-sm">
          No high-risk customers detected. Sentiment monitoring is active.
        </p>
      </div>
    </div>
  );
}
