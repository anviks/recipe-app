<script setup lang="ts">
import { AccountService } from '@/services';
import { ref, watch } from 'vue';
import Notification from '@/components/Notification.vue';
import useAppContext from '@/composables/useAppContext';

const { router, route, authStore, notificationStore } = useAppContext();
const returnPath = ref<string>(route.path);

watch(
  () => route.path,
  (newVal) => {
    if (!['/account/login', '/account/register'].includes(newVal)) {
      returnPath.value = newVal;
    }
  }
);

const logout = async () => {
  try {
    await AccountService.logout(authStore.refreshToken);
  } catch (error) {
    notificationStore.notify('error', 'An error occurred while logging out');
    return;
  }
  
  authStore.clearUserDetails();
  await router.push({ name: 'Login', query: { returnUrl: returnPath.value } });
};
</script>

<template>
  <v-app-bar title="RecipeApp">sdasd</v-app-bar>
  <header>
    <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-light bg-white border-bottom box-shadow mb-3">
      <div class="container-fluid">
        <a
          class="navbar-brand"
          href="/"
          >RecipeApp</a
        >
        <button
          class="navbar-toggler"
          type="button"
          data-bs-toggle="collapse"
          data-bs-target=".navbar-collapse"
          aria-controls="navbarSupportedContent"
          aria-expanded="false"
          aria-label="Toggle navigation"
        >
          <span class="navbar-toggler-icon"></span>
        </button>
        <div class="navbar-collapse collapse d-sm-inline-flex justify-content-between">
          <ul class="navbar-nav flex-grow-1">
            <li class="nav-item">
              <RouterLink
                class="nav-link text-dark"
                :to="{ name: 'Home' }"
                >Home
              </RouterLink>
            </li>
            <li class="nav-item">
              <a
                class="nav-link text-dark"
                href="/Home/Privacy"
                >Privacy</a
              >
            </li>
            <li class="nav-item dropdown">
              <a
                class="nav-link dropdown-toggle"
                href="#"
                role="button"
                data-bs-toggle="dropdown"
                aria-expanded="false"
              >
                Navigation
              </a>
              <ul class="dropdown-menu">
                <li>
                  <!--                  <RouterLink-->
                  <!--                    class="dropdown-item"-->
                  <!--                    :to="{ name: 'Categories' }"-->
                  <!--                  >-->
                  <!--                    Categories-->
                  <!--                  </RouterLink>-->
                  <!--                </li>-->
                  <!--                <li>-->
                  <!--                  <RouterLink-->
                  <!--                    class="dropdown-item"-->
                  <!--                    :to="{ name: 'Ingredients' }"-->
                  <!--                  >-->
                  <!--                    Ingredients-->
                  <!--                  </RouterLink>-->
                  <!--                </li>-->
                  <!--                <li>-->
                  <!--                  <RouterLink-->
                  <!--                    class="dropdown-item"-->
                  <!--                    :to="{ name: 'IngredientTypes' }"-->
                  <!--                  >-->
                  <!--                    Ingredient types-->
                  <!--                  </RouterLink>-->
                  <!--                </li>-->
                  <!--                <li>-->
                  <!--                  <RouterLink-->
                  <!--                    class="dropdown-item"-->
                  <!--                    :to="{ name: 'Recipes' }"-->
                  <!--                  >-->
                  <!--                    Recipes-->
                  <!--                  </RouterLink>-->
                  <!--                </li>-->
                  <!--                <li>-->
                  <!--                  <RouterLink-->
                  <!--                    class="dropdown-item"-->
                  <!--                    :to="{ name: 'Reviews' }"-->
                  <!--                  >-->
                  <!--                    Reviews-->
                  <!--                  </RouterLink>-->
                  <!--                </li>-->
                  <!--                <li>-->
                  <!--                  <RouterLink-->
                  <!--                    class="dropdown-item"-->
                  <!--                    :to="{ name: 'Units' }"-->
                  <!--                  >-->
                  <!--                    Units-->
                  <!--                  </RouterLink>-->
                </li>
              </ul>
            </li>
          </ul>
          <ul
            v-if="!authStore.isAuthenticated"
            class="navbar-nav"
          >
            <li class="nav-item">
              <RouterLink
                class="nav-link text-dark"
                :to="{ name: 'Register', query: { ...route.query, returnUrl: returnPath } }"
                >Register
              </RouterLink>
            </li>
            <li class="nav-item">
              <RouterLink
                class="nav-link text-dark"
                :to="{ name: 'Login', query: { ...route.query, returnUrl: returnPath } }"
                >Login
              </RouterLink>
            </li>
          </ul>
          <ul
            v-else
            class="navbar-nav"
          >
            <li class="nav-item">
              <RouterLink
                class="nav-link text-dark"
                :to="{}"
              >
                Hello, {{ authStore.username }}!
              </RouterLink>
            </li>
            <li class="nav-item">
              <button
                @click="logout"
                class="nav-link text-dark"
                type="button"
              >
                Logout
              </button>
            </li>
          </ul>
        </div>
      </div>
    </nav>
  </header>
</template>

<style scoped></style>