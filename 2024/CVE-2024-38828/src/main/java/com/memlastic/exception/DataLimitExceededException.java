package com.memlastic.exception;

import java.io.IOException;

public class DataLimitExceededException extends IOException {
    public DataLimitExceededException(String text){
        super(text);
    }
}
