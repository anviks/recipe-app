import GenericService from '@/services/genericService';
import type { Unit } from '@/types';

export default class UnitsService extends GenericService<Unit, Unit> {
    protected override getServiceUrl(): string {
        return 'Units/';
    }
}