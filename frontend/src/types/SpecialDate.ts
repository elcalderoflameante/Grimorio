

export interface SpecialDateDto {
  id: string;
  branchId: string;
  date: Date | string;
  name: string;
  description?: string;
}

export interface CreateSpecialDateDto {
  date: Date | string;
  name: string;
  description?: string;
}

export interface UpdateSpecialDateDto {
  date: Date | string;
  name: string;
  description?: string;
}
