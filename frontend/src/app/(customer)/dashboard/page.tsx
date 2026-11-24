// src/app/(customer)/dashboard/page.tsx
import { auth } from "@clerk/nextjs/server";

export default async function DashboardPage() {
  const { userId } = await auth();

  return (
    <div className="max-w-5xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Your Dashboard</h1>
        <p className="text-gray-500 text-sm mt-1">
          Track your applications and account activity
        </p>
      </div>

      {/* KYC Status Banner */}
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 mb-6 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <span className="text-2xl">🪪</span>
          <div>
            <p className="font-semibold text-amber-900 text-sm">Complete your KYC</p>
            <p className="text-amber-700 text-xs">
              Upload your documents to unlock all banking products
            </p>
          </div>
        </div>
        <a
          href="/kyc"
          className="bg-amber-600 text-white px-4 py-2 rounded-lg text-sm font-medium hover:bg-amber-700 transition-colors"
        >
          Complete KYC
        </a>
      </div>

      {/* Stats Row */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-8">
        {[
          { label: "Active Applications", value: "0", icon: "📋", color: "blue" },
          { label: "Notifications", value: "0", icon: "🔔", color: "purple" },
          { label: "Security Level", value: "1 / 3", icon: "🔒", color: "green" },
        ].map((stat) => (
          <div
            key={stat.label}
            className="bg-white rounded-xl border border-gray-200 p-5 flex items-center gap-4"
          >
            <div className="text-3xl">{stat.icon}</div>
            <div>
              <p className="text-2xl font-bold text-gray-900">{stat.value}</p>
              <p className="text-xs text-gray-500">{stat.label}</p>
            </div>
          </div>
        ))}
      </div>

      {/* Quick Actions */}
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <h2 className="font-semibold text-gray-900 mb-4">Quick Actions</h2>
        <div className="grid grid-cols-2 md:grid-cols-3 gap-3">
          {[
            { href: "/apply/savings-account", label: "Open Savings Account", icon: "🏦" },
            { href: "/apply/credit-card", label: "Apply for Credit Card", icon: "💳" },
            { href: "/apply/personal-loan", label: "Personal Loan", icon: "💰" },
            { href: "/apply/fixed-deposit", label: "Open Fixed Deposit", icon: "📈" },
            { href: "/apply/home-loan", label: "Home Loan", icon: "🏠" },
            { href: "/kyc", label: "Upload KYC Docs", icon: "📄" },
          ].map((action) => (
            <a
              key={action.href}
              href={action.href}
              className="flex items-center gap-2 p-3 border border-gray-200 rounded-lg text-sm font-medium text-gray-700 hover:bg-blue-50 hover:border-blue-200 hover:text-blue-700 transition-all"
            >
              <span>{action.icon}</span>
              {action.label}
            </a>
          ))}
        </div>
      </div>

      {/* Recent Applications */}
      <div className="bg-white rounded-xl border border-gray-200 p-6">
        <h2 className="font-semibold text-gray-900 mb-4">Recent Applications</h2>
        <div className="text-center py-12 text-gray-400">
          <div className="text-5xl mb-3">📋</div>
          <p className="text-sm">No applications yet.</p>
          <a
            href="/apply"
            className="text-blue-600 text-sm hover:underline mt-2 inline-block"
          >
            Browse products to get started →
          </a>
        </div>
      </div>
    </div>
  );
}
