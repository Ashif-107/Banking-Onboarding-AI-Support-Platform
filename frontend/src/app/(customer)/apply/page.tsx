// src/app/(customer)/apply/page.tsx
// Product catalog — browse all 6 banking products

import Link from "next/link";

const PRODUCTS = [
  {
    slug: "savings-account",
    name: "Savings Account",
    icon: "🏦",
    tagline: "Zero-fee account with 4.5% interest",
    highlight: "Instant approval",
    color: "blue",
    details: ["No minimum balance", "Free NEFT/IMPS", "Unlimited withdrawals"],
  },
  {
    slug: "credit-card",
    name: "Credit Card",
    icon: "💳",
    tagline: "Lifetime-free card with 2% cashback",
    highlight: "72-hr approval",
    color: "purple",
    details: ["2% cashback on all spends", "No joining fee", "Contactless payments"],
  },
  {
    slug: "personal-loan",
    name: "Personal Loan",
    icon: "💰",
    tagline: "Up to ₹10 lakhs at 10.5% p.a.",
    highlight: "Same-day disbursal",
    color: "green",
    details: ["No collateral required", "Flexible 12–60 months", "Prepay anytime"],
  },
  {
    slug: "fixed-deposit",
    name: "Fixed Deposit",
    icon: "📈",
    tagline: "7.5% p.a. for 1-year tenure",
    highlight: "Guaranteed returns",
    color: "yellow",
    details: ["Start from ₹1,000", "Loan against FD", "Auto-renewal option"],
  },
  {
    slug: "home-loan",
    name: "Home Loan",
    icon: "🏠",
    tagline: "Up to ₹5 crore at 8.5% p.a.",
    highlight: "5-day processing",
    color: "red",
    details: ["30-year tenure", "No prepayment charges", "Balance transfer available"],
  },
  {
    slug: "current-account",
    name: "Current Account",
    icon: "🏢",
    tagline: "Zero-balance business account",
    highlight: "For businesses",
    color: "indigo",
    details: ["Unlimited transactions", "Cheque book free", "GST-ready account"],
  },
];

export default function ApplyPage() {
  return (
    <div className="max-w-5xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900">Banking Products</h1>
        <p className="text-gray-500 text-sm mt-1">
          Our AI pre-screener will check your eligibility before you apply — no hard enquiry.
        </p>
      </div>

      <div className="grid md:grid-cols-2 lg:grid-cols-3 gap-5">
        {PRODUCTS.map((product) => (
          <div
            key={product.slug}
            className="bg-white rounded-2xl border border-gray-200 hover:border-blue-300 hover:shadow-md transition-all p-5 flex flex-col"
          >
            <div className="flex items-start justify-between mb-3">
              <span className="text-4xl">{product.icon}</span>
              <span className="text-xs bg-green-100 text-green-700 font-medium px-2 py-1 rounded-full">
                {product.highlight}
              </span>
            </div>

            <h3 className="font-bold text-gray-900 text-lg mb-1">{product.name}</h3>
            <p className="text-gray-500 text-sm mb-4">{product.tagline}</p>

            <ul className="space-y-1 mb-5 flex-1">
              {product.details.map((d) => (
                <li key={d} className="text-xs text-gray-600 flex items-center gap-2">
                  <span className="text-green-500">✓</span>
                  {d}
                </li>
              ))}
            </ul>

            <Link
              href={`/apply/${product.slug}`}
              className="block text-center bg-blue-700 text-white font-semibold py-2.5 rounded-xl text-sm hover:bg-blue-800 transition-colors"
            >
              Apply Now →
            </Link>
          </div>
        ))}
      </div>
    </div>
  );
}
