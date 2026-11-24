// src/app/page.tsx — Landing Page
import Link from "next/link";
import { SignedIn, SignedOut } from "@clerk/nextjs";

export default function LandingPage() {
  return (
    <main className="min-h-screen bg-gradient-to-br from-blue-900 via-blue-800 to-indigo-900 text-white">
      {/* Nav */}
      <nav className="flex items-center justify-between px-8 py-5 border-b border-blue-700/40">
        <div className="flex items-center gap-3">
          <div className="w-9 h-9 bg-white rounded-lg flex items-center justify-center">
            <span className="text-blue-900 font-black text-sm">IB</span>
          </div>
          <span className="font-bold text-xl tracking-tight">IncedoBank</span>
        </div>
        <div className="flex items-center gap-4">
          <SignedOut>
            <Link
              href="/sign-in"
              className="text-blue-200 hover:text-white transition-colors text-sm"
            >
              Sign In
            </Link>
            <Link
              href="/sign-up"
              className="bg-white text-blue-900 font-semibold px-4 py-2 rounded-lg text-sm hover:bg-blue-50 transition-colors"
            >
              Open Account
            </Link>
          </SignedOut>
          <SignedIn>
            <Link
              href="/dashboard"
              className="bg-white text-blue-900 font-semibold px-4 py-2 rounded-lg text-sm hover:bg-blue-50 transition-colors"
            >
              Go to Dashboard →
            </Link>
          </SignedIn>
        </div>
      </nav>

      {/* Hero */}
      <section className="max-w-5xl mx-auto px-8 py-24 text-center">
        <div className="inline-flex items-center gap-2 bg-blue-800/50 border border-blue-600/50 rounded-full px-4 py-2 text-sm text-blue-200 mb-8">
          <span className="w-2 h-2 bg-green-400 rounded-full animate-pulse" />
          100% Digital · Zero Branch Visits
        </div>

        <h1 className="text-5xl md:text-6xl font-black leading-tight mb-6">
          Banking made
          <br />
          <span className="text-transparent bg-clip-text bg-gradient-to-r from-blue-300 to-cyan-300">
            brilliantly simple
          </span>
        </h1>

        <p className="text-xl text-blue-200 max-w-2xl mx-auto mb-10 leading-relaxed">
          Open an account, apply for loans, credit cards, and FDs — all online.
          AI-powered support available 24/7.
        </p>

        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link
            href="/sign-up"
            className="bg-white text-blue-900 font-bold px-8 py-4 rounded-xl text-lg hover:bg-blue-50 transition-all hover:scale-105 shadow-lg"
          >
            Open Free Account
          </Link>
          <Link
            href="/apply"
            className="border border-blue-400 text-blue-100 font-semibold px-8 py-4 rounded-xl text-lg hover:bg-blue-800/50 transition-all"
          >
            Browse Products
          </Link>
        </div>
      </section>

      {/* Feature Grid */}
      <section className="max-w-5xl mx-auto px-8 pb-24 grid md:grid-cols-3 gap-6">
        {[
          {
            icon: "🔒",
            title: "Secure KYC",
            desc: "AI-powered document verification. Upload once, use everywhere.",
          },
          {
            icon: "⚡",
            title: "Instant Approvals",
            desc: "AI pre-screener gives you approval odds before you apply.",
          },
          {
            icon: "📊",
            title: "Real-time Tracking",
            desc: "Track every step of your application with live status updates.",
          },
          {
            icon: "🤖",
            title: "AI Support",
            desc: "24/7 AI-powered support that understands your banking needs.",
          },
          {
            icon: "📱",
            title: "Omnichannel",
            desc: "Get notified via email, SMS, and in-app — your choice.",
          },
          {
            icon: "🏦",
            title: "6 Products",
            desc: "Savings, Credit Card, Personal Loan, FD, Home Loan & more.",
          },
        ].map((f) => (
          <div
            key={f.title}
            className="bg-blue-800/30 border border-blue-700/40 rounded-2xl p-6 hover:bg-blue-800/50 transition-all"
          >
            <div className="text-3xl mb-3">{f.icon}</div>
            <h3 className="font-bold text-lg mb-2">{f.title}</h3>
            <p className="text-blue-300 text-sm leading-relaxed">{f.desc}</p>
          </div>
        ))}
      </section>
    </main>
  );
}
