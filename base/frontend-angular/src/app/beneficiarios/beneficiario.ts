export type StatusBeneficiario = 'ATIVO' | 'INATIVO';

export interface Beneficiario {
  id: string;
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  status: StatusBeneficiario;
  plano_id: string;
  data_cadastro: string;
}

export interface BeneficiariosPaginados {
  dados: Beneficiario[];
  pagina: number;
  tamanho: number;
  total: number;
}

export interface BeneficiarioRequest {
  nome_completo: string;
  cpf: string;
  data_nascimento: string;
  plano_id: string;
}

export interface BeneficiarioAtualizacaoRequest {
  nome_completo: string;
  data_nascimento: string;
  plano_id: string;
  status: StatusBeneficiario;
}

export interface FiltrosBeneficiarios {
  pagina: number;
  tamanho: number;
  status: StatusBeneficiario | null;
  plano_id: string | null;
}
