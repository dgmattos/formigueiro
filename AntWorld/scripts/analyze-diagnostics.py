#!/usr/bin/env python3
"""Summarize the latest session in bounded JSONL diagnostic files (stdlib only)."""
import argparse
import collections
import json
from pathlib import Path

CAUSES = {
    'EnergyDepletion': 'energia esgotada', 'Starvation': 'fome',
    'Dehydration': 'desidratação', 'ThermalStress': 'estresse térmico',
    'Combat': 'combate', 'Aging': 'envelhecimento', 'Unknown': 'não identificada',
}


def records(path):
    if path.is_dir():
        files = [path / f'simulation.jsonl.{i}' for i in range(9, 0, -1)] + [path / 'simulation.jsonl']
    else:
        files = [path]
    found = False
    for file in files:
        if not file.exists():
            continue
        found = True
        with file.open(encoding='utf-8') as stream:
            for number, line in enumerate(stream, 1):
                try:
                    yield json.loads(line)
                except json.JSONDecodeError:
                    print(f'Aviso: linha incompleta/inválida em {file.name}:{number}.')
    if not found:
        raise FileNotFoundError(f'Nenhum simulation.jsonl em {path}')


def summarize(path):
    session = None
    summary = {}
    for row in records(path):
        if row['sessionId'] != session:
            session = row['sessionId']
            summary = dict(session=session, complete=False, causes=collections.Counter(),
                           stages=collections.Counter(), empty_food={}, deaths=0, extinction=None,
                           last_sample=None, dropped=0)
        summary['last_tick'] = row['tick']
        summary['day'] = row['day']
        summary['progress'] = row['dayProgress']
        kind, data = row['type'], row['data']
        if kind == 'session_started':
            summary['complete'] = True
            summary['seed'] = data['configuration']['seed']
        elif kind == 'ant_died':
            summary['deaths'] += 1
            summary['causes'][data['causes']] += 1
            summary['stages'][data['stage']] += 1
        elif kind == 'population_extinct':
            summary['extinction'] = row
        elif kind == 'population_sample':
            summary['last_sample'] = data
            for colony in data['colonies']:
                if colony['food'] == 0:
                    summary['empty_food'].setdefault(colony['id'], row['simulatedSeconds'])
        elif kind == 'records_dropped':
            summary['dropped'] += data['count']
    if not summary:
        raise ValueError('Log vazio.')
    print(f"Sessão: {summary['session']} · seed: {summary.get('seed', 'fora dos arquivos retidos')}")
    print(f"Último tick: {summary['last_tick']} · dia {summary['day']} · progresso {summary['progress']:.1%}")
    if not summary['complete']:
        print('Histórico parcial: o início da sessão não está nos arquivos fornecidos.')
    print(f"Mortes com registro individual disponível: {summary['deaths']}")
    for cause, count in summary['causes'].most_common():
        label = ' + '.join(CAUSES.get(part, part) for part in cause.split(', '))
        print(f'  {count}: {label}')
    print('Fases:', ', '.join(f'{stage}={count}' for stage, count in summary['stages'].items()) or 'sem mortes')
    for colony, seconds in summary['empty_food'].items():
        print(f'Colônia {colony}: primeira amostra com alimento zero em {seconds:.0f}s simulados.')
    if summary['extinction']:
        row = summary['extinction']
        print(f"Extinção em {row['simulatedSeconds']:.2f}s simulados, tick {row['tick']}, dia {row['day']} ({row['dayProgress']:.1%}).")
    if summary['last_sample']:
        print(f"População na última amostra: {summary['last_sample']['population']}")
    if summary['dropped']:
        print(f"Atenção: {summary['dropped']} registros descartados por limite do buffer.")
    print('Causas representam os fatores ativos no dano fatal; não todo o histórico de danos do indivíduo.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('path', type=Path, help='diretório diagnostics ou arquivo simulation.jsonl')
    arguments = parser.parse_args()
    try:
        summarize(arguments.path.expanduser())
    except (OSError, ValueError, KeyError) as error:
        parser.exit(1, f'Não foi possível analisar: {error}\n')
