import { test, expect } from "@playwright/test";

test.describe("Autenticación", () => {
  test("login exitoso como Admin redirige al dashboard y muestra bienvenida", async ({
    page,
  }) => {
    await page.goto("/login");

    await page.fill("#email", "admin@academia.com");
    await page.fill("#password", "Admin123!");
    await page.click('button[type="submit"]');

    await expect(page).toHaveURL("/dashboard", { timeout: 10_000 });

    // Welcome banner — first name is dynamic so use a partial match
    await expect(page.getByText(/¡Bienvenido,/)).toBeVisible({ timeout: 10_000 });
  });

  test("contraseña vacía muestra error de validación del formulario sin redirigir", async ({
    page,
  }) => {
    await page.goto("/login");

    // Valid email passes browser native validation; empty password has no
    // required attr so browser doesn't block — zod fires and shows inline error
    await page.fill("#email", "admin@academia.com");
    // leave #password empty
    await page.click('button[type="submit"]');

    await expect(page.getByText("La contraseña es requerida")).toBeVisible();
    await expect(page).toHaveURL("/login");
  });
});
