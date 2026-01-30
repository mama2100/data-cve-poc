import json
import requests
from colorist import Color
from getpass import getpass
from prettytable import PrettyTable
from core.common.static.constants import MICROCLAUDIA_DOMAIN, MICROCLAUDIA_ENDPOINTS_PATH
from core.modules.microclaudia.microclaudia_tokens import MicroClaudiaTokens


class MicroClaudiaSignIn:

    def __init__(self):
        self.domain = MICROCLAUDIA_DOMAIN
        self.endpoints = self.json_endpoints()
        self.tokens = MicroClaudiaTokens()
        self.token = None

    @staticmethod
    def json_endpoints() -> dict:
        """
        This function, return a json with the proper detected microclaudia endpoints
        :return:
        """

        with open(MICROCLAUDIA_ENDPOINTS_PATH, 'r', encoding='utf-8') as json_file:
            endpoints_data = json.load(json_file)
        return endpoints_data

    def table_endpoints(self) -> None:
        """
        This function, print detected microclaudia endpoints
        :return:
        """

        # Table
        table = PrettyTable()
        table.field_names = ["Endpoint", "Method", "URI"]
        table.align["URI"] = "l"

        # Endpoints
        for endpoint_element in self.endpoints:
            # Status
            if endpoint_element['status']:
                table.add_row([f'{Color.BLUE}{endpoint_element["name"]}{Color.OFF}', f'{Color.YELLOW}{endpoint_element["method"]}{Color.OFF}', endpoint_element["uri"]])

        # Print
        print(table)
        print('Parameters: ?size=&?groupBy=&?sortBy=&?orderBy=&?filter=&?defaultFilter=&?page=')
        print('\n')

    def signin_website(self) -> None:
        """
        This function, login in microclaudia console
        :return:
        """

        # Ask the user
        user = input("Enter the username (user@test.local): ")
        password = getpass("Enter the password (********): ")

        # Data
        url = f'https://{self.domain}/api/login'
        data = {'username': user, 'password': password}

        # Request
        print('Request to:', url)
        response = requests.post(url, json=data)
        if response.status_code == 200:
            response_data = response.json()
            self.token = response_data.get("token")
            # Decode Payload
            self.tokens.decode_payload_token(self.token)
        else:
            print(f'{Color.RED}An error occurred while sending the request{Color.OFF}')
            print(f'{Color.RED}Status Code: {response.status_code}{Color.OFF}')

    def get_agencies_data(self) -> dict:
        """
        This function, get agencies data
        :return:
        """

        # Url
        headers = {"Authorization": "Bearer " + self.token}
        url_instances = f'https://{self.domain}/api/instances'

        # Requests
        print('Request to:', url_instances)
        response = requests.get(url_instances, headers=headers)
        if response.status_code == 200:
            response_data = response.json()

            # Instances
            instance = None
            for instances in response_data['collection']:
                if self.domain in instances['server']:
                    instance = instances['id']
                    break
            # No instance associated
            if instance is None:
                print(f'{Color.RED}Error, no instance found associated with the provided domain{Color.OFF}')
                print(f'{Color.YELLOW}{json.dumps(response_data, indent=2)}\n{Color.OFF}')
                return {}

            # Agencies
            url_agencies = f'https://{self.domain}/api/instances/{instance}/agencies/all?size=100'
            print('Request to:', url_agencies)
            response = requests.get(url_agencies, headers=headers)
            if response.status_code == 200:
                response_data = response.json()
                return response_data

            # Error
            else:
                print(f'{Color.RED}An error occurred while sending the request{Color.OFF}')
                print(f'{Color.RED}Status Code: {response.status_code}{Color.OFF}')
                return {}

        # Error
        else:
            print(f'{Color.RED}An error occurred while sending the request{Color.OFF}')
            print(f'{Color.RED}Status Code: {response.status_code}{Color.OFF}')
            return {}
