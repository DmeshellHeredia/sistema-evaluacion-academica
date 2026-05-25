import { test, expect } from "@playwright/test";
import { loginAsStudent } from "./helpers/auth";

// juan.perez@academia.com — IngenieriaEnSistemas, sem 3 (live DB)
test.describe("Estudiante — catálogo de materias", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsStudent(page);
    await page.goto("/dashboard/catalog");
    // Wait for auth hydration + layout render (sidebar link appears after hydration)
    await expect(page.getByRole("link", { name: "Mis Materias" })).toBeVisible({
      timeout: 20_000,
    });
  });

  test("página carga sin skeleton infinito y muestra encabezado", async ({ page }) => {
    // Page h1 heading visible in main content (sidebar link confirmed by beforeEach)
    await expect(page.getByRole("main").getByRole("heading", { name: "Mis Materias" })).toBeVisible();

    // Career + semester subtitle loads after studentData query resolves
    // Format: "{career} · Semestre {n}"
    await expect(
      page.getByText(/·\s*Semestre\s*\d+/)
    ).toBeVisible({ timeout: 15_000 });

    // Skeleton rows gone
    await expect(page.locator(".animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });
  });

  test("catálogo carga sin error (muestra materias o estado vacío)", async ({ page }) => {
    // Wait for skeleton to clear
    await expect(page.locator(".animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });

    // After loading, a .bg-card is present whether catalog is empty or not
    await expect(page.locator(".bg-card").first()).toBeVisible({ timeout: 10_000 });
  });

  test("NO muestra materias de otras carreras (CIB)", async ({ page }) => {
    await expect(page.locator(".animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });

    // Ciberseguridad subjects should not appear
    await expect(page.getByText(/CIB-FUN1/)).not.toBeVisible();
    await expect(page.getByText(/CIB-HAK1/)).not.toBeVisible();
  });

  test("materias aprobadas muestran estado 'Aprobada'", async ({ page }) => {
    // estIS1 has IS-MAT1 grade 9.0 (approved)
    await expect(page.locator(".animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });

    await expect(page.getByText("Aprobada").first()).toBeVisible({ timeout: 10_000 });
  });

  test("sidebar muestra 'Mis Materias' y NO muestra 'Profesores'", async ({ page }) => {
    await expect(page.getByRole("link", { name: "Mis Materias" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Profesores" })).not.toBeVisible();
    // Períodos also hidden for students
    await expect(page.getByRole("link", { name: "Períodos" })).not.toBeVisible();
  });

  test("sidebar estudiante no muestra 'Promedios'", async ({ page }) => {
    await expect(page.getByRole("link", { name: "Promedios" })).not.toBeVisible();
  });
});
