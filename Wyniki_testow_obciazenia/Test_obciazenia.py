import psutil
import pandas as pd
import time
from datetime import datetime

def monitor_system(interval=0.5, duration=600):
    data = []
    prev_disk = psutil.disk_io_counters()
    prev_net = psutil.net_io_counters()
    start_time = time.time()

    while time.time() - start_time < duration:
        # Zbieranie danych systemowych
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        cpu_percent_all = psutil.cpu_percent(interval=interval)  # Użycie CPU w procentach
        cpu_percent = psutil.cpu_percent(interval=None, percpu=True)  # Użycie CPU na poszczególne rdzenie
        memory = psutil.virtual_memory()
        disk = psutil.disk_io_counters()
        net = psutil.net_io_counters()

        # Obliczanie prędkości odczytu i zapisu na dysku
        disk_read = (disk.read_bytes - prev_disk.read_bytes) / (1024 ** 2) / interval  # MB/s
        disk_write = (disk.write_bytes - prev_disk.write_bytes) / (1024 ** 2) / interval  # MB/s

        # Obliczanie prędkości wysyłania i odbierania danych w sieci
        net_sent = (net.bytes_sent - prev_net.bytes_sent) / (1024 ** 2) / interval  # MB/s
        net_recv = (net.bytes_recv - prev_net.bytes_recv) / (1024 ** 2) / interval  # MB/s

        # Obliczanie zużycia pamięci RAM
        memory_used_gb = (memory.total - memory.available) / (1024 ** 3)  # GB

        # Przygotowywanie danych dotyczących użycia CPU na poszczególnych wątkach
        cpu_thread_data = {
            f'CPU Thread {i + 1} (%)': cpu_percent[i] if i < len(cpu_percent) else None
            for i in range(len(cpu_percent))  # Obsługuje różną liczbę rdzeni
        }

        # Dodawanie danych tylko w przypadku, gdy użycie CPU jest większe niż 0
        if cpu_percent_all > 0:
            row = {
                'Timestamp': timestamp,
                'CPU Usage Overall (%)': cpu_percent_all,
                **cpu_thread_data,  # Dane dla poszczególnych wątków CPU
                'Memory Usage (%)': memory.percent,
                'Memory Total (GB)': memory.total / (1024 ** 3),  # GB
                'Memory Available (GB)': memory.available / (1024 ** 3),  # GB
                'Memory Used (GB)': memory_used_gb,  # Zużyta pamięć RAM w GB
                'Disk Read (MB/s)': disk_read,
                'Disk Write (MB/s)': disk_write,
                'Network Sent (MB/s)': net_sent,
                'Network Received (MB/s)': net_recv,
            }
            data.append(row)

        # Aktualizacja wartości referencyjnych dla dysków i sieci
        prev_disk = disk
        prev_net = net

    # Zapis danych do pliku Excel
    df = pd.DataFrame(data)
    df.to_excel('system_monitoring_data.xlsx', index=False, engine='openpyxl')
    print("Zakończono zbieranie danych. Dane zapisane do 'system_monitoring_data.xlsx'.")

monitor_system(interval=0.5, duration=600)
