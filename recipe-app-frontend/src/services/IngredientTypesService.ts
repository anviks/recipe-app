import type { IngredientType } from '@/types';
import GenericService from '@/services/genericService';

export default class IngredientTypesService extends GenericService<IngredientType, IngredientType> {
    protected override getServiceUrl(): string {
        return 'IngredientTypes/';
    }
}