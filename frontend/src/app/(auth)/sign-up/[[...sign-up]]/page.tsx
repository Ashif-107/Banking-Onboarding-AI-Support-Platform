// src/app/(auth)/sign-up/[[...sign-up]]/page.tsx
import { SignUp } from "@clerk/nextjs";

export default function SignUpPage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-900 to-indigo-900 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-14 h-14 bg-white rounded-2xl mb-4 shadow-lg">
            <span className="text-blue-900 font-black text-xl">IB</span>
          </div>
          <h1 className="text-white text-2xl font-bold">Open your account</h1>
          <p className="text-blue-200 text-sm mt-1">Join IncedoBank in under 5 minutes</p>
        </div>
        <SignUp
          appearance={{
            elements: {
              rootBox: "mx-auto",
              card: "shadow-2xl rounded-2xl border-0",
            },
          }}
        />
      </div>
    </div>
  );
}
