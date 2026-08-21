import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { API_BASE } from '../nucleo/api';
import {
  Beneficiario,
  BeneficiarioAtualizacaoRequest,
  BeneficiarioRequest,
  BeneficiariosPaginados,
  FiltrosBeneficiarios,
} from './beneficiario';

@Injectable({ providedIn: 'root' })
export class BeneficiarioServico {
  private readonly http = inject(HttpClient);
  private readonly base = inject(API_BASE);

  listar(filtros: FiltrosBeneficiarios): Observable<BeneficiariosPaginados> {
    let params = new HttpParams().set('pagina', filtros.pagina).set('tamanho', filtros.tamanho);

    if (filtros.status) {
      params = params.set('status', filtros.status);
    }

    if (filtros.plano_id) {
      params = params.set('plano_id', filtros.plano_id);
    }

    return this.http.get<BeneficiariosPaginados>(`${this.base}/beneficiarios`, { params });
  }

  criar(dados: BeneficiarioRequest): Observable<Beneficiario> {
    return this.http.post<Beneficiario>(`${this.base}/beneficiarios`, dados);
  }

  atualizar(id: string, dados: BeneficiarioAtualizacaoRequest): Observable<Beneficiario> {
    return this.http.put<Beneficiario>(`${this.base}/beneficiarios/${id}`, dados);
  }

  excluir(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/beneficiarios/${id}`);
  }
}
