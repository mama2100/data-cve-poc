import json
import base64
from datetime import datetime, timezone
from colorist import Color


class MicroClaudiaTokens:

    @staticmethod
    def decode_payload_token(bearer_token: str) -> None:
        """
        This function, decode and print JSON Web Token payload
        :param bearer_token:
        :return:
        """

        try:

            # Bearer Token
            bearer_token_parts = bearer_token.split('.')
            print(f'\n{Color.BLUE}JSON Web Token / JSON Web Signature: {Color.YELLOW}{bearer_token_parts[0]}{Color.BLUE}.{Color.GREEN}{bearer_token_parts[1]}{Color.BLUE}.{Color.RED}{bearer_token_parts[2]}{Color.OFF}\n{Color.BLUE}JWT/JWS (RFC7519): {Color.YELLOW}Header{Color.BLUE}.{Color.GREEN}Payload{Color.BLUE}.{Color.RED}Signature{Color.OFF}\n')

            # Decode Payload
            bearer_token_decoded_payload = base64.urlsafe_b64decode(bearer_token_parts[1] + '=' * (4 - len(bearer_token_parts[1]) % 4)).decode('utf-8')
            bearer_token_json_body = json.loads(bearer_token_decoded_payload)
            print(f'{Color.BLUE}Decode Payload: \n{Color.GREEN}' + json.dumps(bearer_token_json_body, indent=2) + f'{Color.OFF}\n')

            # Username
            print(f'{Color.BLUE}Username: {Color.GREEN}{bearer_token_json_body.get("sub")}\n{Color.OFF}')

            # Datetime
            bearer_token_iat_datetime = datetime.fromtimestamp(bearer_token_json_body.get('iat', 0), tz=timezone.utc)
            bearer_token_iat_datetime_str = bearer_token_iat_datetime.strftime('%Y-%m-%d %H:%M:%S UTC')
            bearer_token_exp_datetime = datetime.fromtimestamp(bearer_token_json_body.get('exp', 0), tz=timezone.utc)
            bearer_token_exp_datetime_str = bearer_token_exp_datetime.strftime('%Y-%m-%d %H:%M:%S UTC')
            bearer_token_duration = bearer_token_exp_datetime - bearer_token_iat_datetime
            print(f'{Color.BLUE}Issued At (iat): {Color.GREEN}{bearer_token_iat_datetime_str}{Color.OFF}')
            print(f'{Color.BLUE}Expiration Time (exp): {Color.GREEN}{bearer_token_exp_datetime_str}{Color.OFF}')
            print(f'{Color.BLUE}Token Duration: {Color.GREEN}{bearer_token_duration}{Color.OFF}\n')

            # Message
            print(
                f'{Color.YELLOW}You can use the token to make requests or try the PoC{Color.OFF}\nDownload Insomnia (https://insomnia.rest/) or Postman (https://www.postman.com/)\n')

        except Exception as e:
            print(f'Error decoding token payload: {e}\n')

    @staticmethod
    def check_payload_token(bearer_token: str) -> None:
        """
        This function, verifies if token has expired
        :param bearer_token:
        :return:
        """

        try:

            # Bearer Token
            bearer_token_parts = bearer_token.split('.')
            decoded_payload = base64.urlsafe_b64decode(
                bearer_token_parts[1] + '=' * (4 - len(bearer_token_parts[1]) % 4)).decode('utf-8')
            payload_json = json.loads(decoded_payload)

            # Expiration and Username
            username = payload_json.get('sub')
            expired = payload_json.get('exp')
            current_time = datetime.now()
            expired_datetime = datetime.fromtimestamp(expired)

            # Info
            if current_time > expired_datetime:
                print(f'{Color.RED}Token has expired{Color.OFF}')
                print(f'{Color.BLUE}{username}{Color.OFF}')
            else:
                print(f'{Color.GREEN}Token is still valid{Color.OFF}')
                print(f'{Color.BLUE}{username}{Color.OFF}')

        except Exception as e:
            print(f'Error decoding token payload: {e}\n')
