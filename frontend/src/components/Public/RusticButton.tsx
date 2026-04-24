import type { ReactNode } from 'react';

interface RusticButtonProps {
  children?: ReactNode;
  onClick?: () => void;
  className?: string;
}

export default function RusticButton({ children, onClick, className = '' }: RusticButtonProps) {
  return (
    <div className={`relative p-1.5 ${className}`}>
      <button
        onClick={onClick}
        className="relative flex h-full w-full items-center justify-center rounded-xl border-4 border-[#8B5E3C] bg-[#D2B48C] bg-opacity-95 p-3 shadow-[inset_0_2px_4px_rgba(255,255,255,0.4),_inset_0_-2px_4px_rgba(0,0,0,0.3),_0_2px_4px_rgba(0,0,0,0.2)] transition-all duration-150 hover:scale-[1.02] hover:bg-[#C9A67C] active:scale-[0.98] active:border-[#734A31] active:shadow-[inset_0_2px_4px_rgba(0,0,0,0.5),_inset_0_-2px_4px_rgba(255,255,255,0.1),_0_2px_4px_rgba(0,0,0,0.2)]"
        style={{
          borderStyle: 'outset',
          borderColor: '#8B5E3C',
        }}
      >
        <div className="pointer-events-none absolute inset-0 overflow-hidden rounded-lg opacity-40">
          <div className="animate-flicker absolute inset-0 bg-[radial-gradient(ellipse_at_center,_rgba(255,255,100,0.2),_rgba(255,150,50,0.1),_transparent_70%)]" />
        </div>

        <div className="relative z-10 flex h-full w-full items-center justify-center">
          {children}
        </div>
      </button>
    </div>
  );
}
