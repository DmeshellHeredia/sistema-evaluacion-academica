import { test, expect } from "@playwright/test";
import { loginAsAdmin } from "./helpers/auth";

test.describe("Estudiantes", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await page.goto("/dashboard/students");
    // Wait for the table to finish loading (skeleton rows disappear)
    await expect(page.locator("table tbody .animate-pulse").first()).not.toBeVisible({
      timeout: 15_000,
    });
  });

  test("tabla carga y muestra estudiantes sembrados", async ({ page }) => {
    // Table headers present
    await expect(page.getByText("Código")).toBeVisible();
    await expect(page.getByText("Nombre")).toBeVisible();

    // At least one row with a student code (EST-YYYY-XXXX)
    await expect(
      page.locator("td").filter({ hasText: /^EST-/ }).first()
    ).toBeVisible();
  });

  test("búsqueda server-side filtra estudiantes por nombre", async ({ page }) => {
    const searchInput = page.getByPlaceholder("Buscar por nombre, código o carrera...");

    await searchInput.fill("juan");

    // Debounce is 400 ms — wait for the table to update
    await page.waitForTimeout(600);

    // Should show at least one result matching the seeded "Juan Pérez"
    await expect(
      page.locator("td").filter({ hasText: /Juan/i }).first()
    ).toBeVisible({ timeout: 10_000 });
  });

  test("búsqueda sin coincidencias muestra mensaje vacío", async ({ page }) => {
    const searchInput = page.getByPlaceholder("Buscar por nombre, código o carrera...");

    await searchInput.fill("xqznotexists99999");

    await page.waitForTimeout(600);

    await expect(
      page.getByText("No se encontraron resultados para tu búsqueda")
    ).toBeVisible({ timeout: 10_000 });
  });
});
