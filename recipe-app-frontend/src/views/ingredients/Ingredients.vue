<script setup lang="ts">
import type { Ingredient, ResultObject } from '@/types';
import { onMounted, ref } from 'vue';
import useServices from '@/helpers/useServices';

const { ingredientsService } = useServices();
const ingredients = ref<Ingredient[]>([]);

onMounted(async () => {
    const result = await ingredientsService.findAll();
    ingredients.value = result.data!;
});
</script>

<template>
    <h1>Ingredients</h1>
    <p>
        <RouterLink :to="{name: 'IngredientCreate'}">Create New</RouterLink>
    </p>
    <table v-if="ingredients.length" class="table">
        <thead>
        <tr>
            <th>
                Name
            </th>
            <th>
                Types
            </th>
            <th></th>
        </tr>
        </thead>
        <tbody>
        <tr v-for="ingredient in ingredients" :key="ingredient.id">
            <td>
                {{ ingredient.name }}
            </td>
            <td>
                {{ ingredient.ingredientTypeAssociations?.length }}
            </td>
            <td>
                <RouterLink :to="{name: 'IngredientEdit', params: {id: ingredient.id}}">Edit</RouterLink> |
                <RouterLink :to="{name: 'IngredientDetails', params: {id: ingredient.id}}">Details</RouterLink> |
                <RouterLink :to="{name: 'IngredientDelete', params: {id: ingredient.id}}">Delete</RouterLink>
            </td>
        </tr>
        </tbody>
    </table>
    <span v-else>Loading...</span>
</template>

<style scoped></style>