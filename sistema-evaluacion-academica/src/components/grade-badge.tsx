import { cn } from "@/lib/utils"

const categoryConfig = {
  Excelente: {
    bg: "bg-emerald-100 border-emerald-200",
    text: "text-emerald-700",
    dot: "bg-emerald-500",
  },
  Buena: {
    bg: "bg-blue-100 border-blue-200",
    text: "text-blue-700",
    dot: "bg-blue-500",
  },
  "Por mejorar": {
    bg: "bg-red-100 border-red-200",
    text: "text-red-700",
    dot: "bg-red-500",
  },
} as const

type Category = keyof typeof categoryConfig

interface GradeBadgeProps {
  category: string
  value?: number
  size?: "sm" | "md"
}

export function GradeBadge({ category, value, size = "md" }: GradeBadgeProps) {
  const config = categoryConfig[category as Category] ?? {
    bg: "bg-muted border-border",
    text: "text-muted-foreground",
    dot: "bg-muted-foreground",
  }

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border font-medium",
        config.bg,
        config.text,
        size === "sm" ? "px-2 py-0.5 text-xs" : "px-2.5 py-1 text-xs"
      )}
    >
      <span className={cn("h-1.5 w-1.5 rounded-full shrink-0", config.dot)} />
      {value !== undefined && <span className="font-bold">{value.toFixed(1)}</span>}
      {category}
    </span>
  )
}
