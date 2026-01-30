import os
import re
import sqlite3
from colorist import Color
from datetime import datetime, timedelta
from core.common.static.constants import MICROCLAUDIA_DOMAIN


class HistoryBrowserObtain:

    def __init__(self):
        self.domain = MICROCLAUDIA_DOMAIN
        self.secrets = []

    def firefox(self) -> None:
        """
        This function, obtain Firefox browser history
        :return:
        """

        try:

            # Path to the Firefox history database on Windows
            firefox_profile_path = os.path.expanduser("~\\AppData\\Roaming\\Mozilla\\Firefox\\Profiles\\")
            profile_folders = os.listdir(firefox_profile_path)

            # First profile
            first_profile_folder = profile_folders[0]
            firefox_history_db = os.path.join(firefox_profile_path, first_profile_folder, "places.sqlite")

            # Connect to the database
            conn = sqlite3.connect(firefox_history_db)
            cursor = conn.cursor()

            # Regular expression to search for SHA-256 hash in a URL
            sha256_regex = re.compile(r'[0-9a-f]{64}')

            # SQL query to get the history from the last year
            one_year_ago = datetime.now() - timedelta(days=365)
            query = f"SELECT url, title FROM moz_places WHERE last_visit_date >= {one_year_ago.timestamp() * 1000000} ORDER BY last_visit_date DESC;"
            cursor.execute(query)

            # Get the filtered results
            results = [row for row in cursor.fetchall() if self.domain in row[0] and sha256_regex.search(row[0])]

            # Filtered history
            for row in results:
                url, title = row
                message = f'firefox - {title} - {url}'
                if message not in self.secrets:
                    self.secrets.append(message)

            # Close the connection to the database
            conn.close()

        except Exception as e:
            print(f'Error obtaining firefox history: {e}')

    def chrome(self) -> None:
        """
        This function, obtain Google Chrome browser history
        :return:
        """

        try:

            # Path to the Chrome history database on Windows
            chrome_history_path = os.path.expanduser("~\\AppData\\Local\\Google\\Chrome\\User Data\\Default\\History")

            # Connect to the database
            conn = sqlite3.connect(chrome_history_path)
            cursor = conn.cursor()

            # Regular expression to search for SHA-256 hash in a URL
            sha256_regex = re.compile(r'[0-9a-f]{64}')

            # SQL query to get the history from the last year
            one_year_ago = datetime.now() - timedelta(days=365)
            query = f"SELECT url, title FROM urls WHERE last_visit_time >= {one_year_ago.timestamp()} * 1000000 ORDER BY last_visit_time DESC;"
            cursor.execute(query)

            # Get the filtered results
            results = [row for row in cursor.fetchall() if self.domain in row[0] and sha256_regex.search(row[0])]

            # Show the filtered history
            for row in results:
                url, title = row
                message = f'chrome - {title} - {url}'
                if message not in self.secrets:
                    self.secrets.append(message)

            # Close the connection to the database
            conn.close()

        except Exception as e:
            print(e)
            print(f'Error obtaining chrome history: {e}')

    def edge(self) -> None:
        """
        This function, obtain Microsoft Edge browser history
        :return:
        """

        try:

            # Path to the Edge history database on Windows
            edge_history_path = os.path.expanduser("~\\AppData\\Local\\Microsoft\\Edge\\User Data\\Default\\History")

            # Connect to the database
            conn = sqlite3.connect(edge_history_path)
            cursor = conn.cursor()

            # Regular expression to search for SHA-256 hash in a URL
            sha256_regex = re.compile(r'[0-9a-f]{64}')

            # SQL query to get the history from the last year
            one_year_ago = datetime.now() - timedelta(days=365)
            query = f"SELECT url, title FROM urls WHERE last_visit_time >= {one_year_ago.timestamp()} * 1000000 ORDER BY last_visit_time DESC;"
            cursor.execute(query)

            # Get the filtered results
            results = [row for row in cursor.fetchall() if self.domain in row[0] and sha256_regex.search(row[0])]

            # Show the filtered history
            for row in results:
                url, title = row
                message = f'edge - {title} - {url}'
                if message not in self.secrets:
                    self.secrets.append(message)

            # Close the connection to the database
            conn.close()

        except Exception as e:
            print(f'Error obtaining edge history: {e}')

    def browsers(self) -> None:
        """
        This function, obtain common browsers history
        :return:
        """

        # Message
        print(f'{Color.YELLOW}Obtaining History:{Color.OFF}')

        # History
        self.firefox()
        self.chrome()
        self.edge()

        # Print
        for data in self.secrets:
            print(data)
        print('\n')
