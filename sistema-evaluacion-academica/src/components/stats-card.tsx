import { cn } from "@/lib/utils"
import type { LucideIcon } from "lucide-react"

interface StatsCardProps {
  title: string
  value: string | number
  subtitle?: string
  icon: LucideIcon
  trend?: { value: number; label: string }
  color?: "indigo" | "emerald" | "blue" | "violet" | "red" | "yellow" | "green"
}

const colorMap = {
  indigo: {
    icon: "bg-primary/10 text-primary",
    badge: "bg-primary/10 text-primary",
  },
  emerald: {
    icon: "bg-emerald-100 text-emerald-600",
    badge: "bg-emerald-100 text-emerald-600",
  },
  blue: {
    icon: "bg-blue-100 text-blue-600",
    badge: "bg-blue-100 text-blue-600",
  },
  violet: {
    icon: "bg-violet-100 text-violet-600",
    badge: "bg-violet-100 text-violet-600",
  },
  red: {
    icon: "bg-red-100 text-red-600",
    badge: "bg-red-100 text-red-600",
  },
  yellow: {
    icon: "bg-yellow-100 text-yellow-600",
    badge: "bg-yellow-100 text-yellow-600",
  },
  green: {
    icon: "bg-green-100 text-green-600",
    badge: "bg-green-100 text-green-600",
  },
}

export function StatsCard({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  color = "indigo",
}: StatsCardProps) {
  const colors = colorMap[color]

  return (
    <div className="bg-card border border-border rounded-2xl p-5 shadow-sm hover:shadow-md transition-shadow">
      <div className="flex items-start justify-between">
        <div className="space-y-1">
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="text-2xl font-bold text-foreground">{value}</p>
          {subtitle && (
            <p className="text-xs text-muted-foreground">{subtitle}</p>
          )}
        </div>
        <div className={cn("flex h-10 w-10 items-center justify-center rounded-xl", colors.icon)}>
          <Icon className="h-5 w-5" />
        </div>
      </div>

      {trend && (
        <div className="mt-3 pt-3 border-t border-border">
          <span
            className={cn(
              "text-xs font-medium px-2 py-0.5 rounded-full",
              colors.badge
            )}
          >
            {trend.value > 0 ? "+" : ""}{trend.value}% {trend.label}
          </span>
        </div>
      )}
    </div>
  )
}
