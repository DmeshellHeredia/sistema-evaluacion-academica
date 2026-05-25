import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "./helpers/auth";

test.describe("Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/dashboard");
  });

  test("muestra la navegación principal en el sidebar", async ({ page }) => {
    // Admin sidebar: administrative modules only, no Calificaciones
    await expect(page.getByRole("link", { name: "Tablero" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Estudiantes", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Materias", exact: true })).toBeVisible();
    await expect(page.getByRole("link", { name: "Períodos", exact: true })).toBeVisible();
    // Calificaciones is NOT in Admin sidebar (only Profesor and Estudiante)
    await expect(page.getByRole("link", { name: "Calificaciones" })).not.toBeVisible();
  });

  test("muestra las tarjetas de estadísticas cargadas desde el backend", async ({
    page,
  }) => {
    // Stats cards rendered after data loads — wait for spinner to disappear
    await expect(page.locator(".animate-spin").first()).not.toBeVisible({
      timeout: 15_000,
    });

    // Stats cards contain their titles (new admin dashboard labels)
    await expect(page.getByText("Estudiantes").first()).toBeVisible();
    await expect(page.getByText("Materias").first()).toBeVisible();
  });
});
